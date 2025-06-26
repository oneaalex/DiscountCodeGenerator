using Serilog;
using System.Collections.Concurrent;
using DiscountCodeApplication.Services.Interfaces;
using DiscountCodeApplication.Repository;
using DiscountCodeApplication.Models;
using DiscountCodeApplication.UnitOfWork;

namespace DiscountCodeApplication.Services
{

    public class DiscountCodeService(IDiscountCodeRepository codeRepository, IDiscountCodeGenerator codeGenerator, IUnitOfWork unitOfWork) : IDiscountCodeService
    {
        private static readonly SemaphoreSlim _generateLock = new(1, 1);

        // Add this for per-code locking
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _codeLocks = new();

        public async Task<bool> GenerateAndAddCodesAsync(ushort count, byte length)
        {
            await _generateLock.WaitAsync();
            try
            {
                Log.Information("Generating {Count} discount codes of length {Length}", count, length);

                var existingCodes = await codeRepository.GetAllCodesAsync(); // Get existing codes to avoid duplicates
                List<string> codes = codeGenerator.GenerateCodes(count, length, existingCodes);
                List<DiscountCode> discountCodes = [];

                foreach (string code in codes)
                {
                    discountCodes.Add(new DiscountCode { Code = code });
                }

                await codeRepository.AddRangeDiscountCodeAsync(discountCodes);

                await unitOfWork.CompleteAsync(); // Save changes
                Log.Information("Generated and added {Count} discount codes", count);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to add generated discount codes");
                return false;
            }
            finally
            {
                _generateLock.Release();
            }
        }

        public Task<List<string>> GetAllCodesAsync()
        {
            //GetAllCachedCodesFromRedisAsync(); // Uncomment if using Redis caching
            Log.Information("Retrieving all discount codes");
            return codeRepository.GetAllCodesAsync();
        }

        public Task<List<string>> GetMostRecentCodesAsync(int count = 10)
        {
            Log.Information("Retrieving the most recent {Count} discount codes", count);
            return codeRepository.GetMostRecentCodesAsync(count);
        }

        public async Task<byte> UseCodeAsync(string code)
        {
            var codeLock = _codeLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await codeLock.WaitAsync();
            try
            {
                Log.Information("Attempting to use discount code '{Code}'", code);
                var discountCode = await codeRepository.GetDiscountCodeByCodeAsync(code);

                if (discountCode == null)
                {
                    Log.Warning("Discount code '{Code}' not found", code);
                    return (byte)UseCodeResultEnum.Failure;
                }

                if (discountCode.IsDeleted)
                {
                    Log.Warning("Discount code '{Code}' is deleted", code);
                    return (byte)UseCodeResultEnum.Deleted;
                }

                if (!discountCode.IsActive)
                {
                    Log.Warning("Discount code '{Code}' is inactive", code);
                    return (byte)UseCodeResultEnum.Inactive;
                }

                if (discountCode.IsUsed)
                {
                    Log.Warning("Discount code '{Code}' is already used", code);
                    return (byte)UseCodeResultEnum.AlreadyUsed;
                }

                if (discountCode.ExpirationDate < DateTime.UtcNow)
                {
                    Log.Warning("Discount code '{Code}' is expired", code);
                    return (byte)UseCodeResultEnum.Expired;
                }

                discountCode.IsUsed = true;
                await codeRepository.UpdateDiscountCodeAsync(discountCode);
                await unitOfWork.CompleteAsync();
                Log.Information("Discount code '{Code}' marked as used", code);
                return (byte)UseCodeResultEnum.Success;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error using discount code '{Code}'", code);
                return (byte)UseCodeResultEnum.Exception;
            }
            finally
            {
                codeLock.Release();
                if (codeLock.CurrentCount == 1)
                    _codeLocks.TryRemove(code, out _);
            }
        }
    }
}
