using DiscountCodeApplication.Redis;
using DiscountCodeApplication.DB;
using DiscountCodeApplication.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DiscountCodeApplication.Repository
{
    public class DiscountCodeRepository(DiscountCodeContext context, ICacheService cacheService) : IDiscountCodeRepository
    {
        private const int RECENT_CODES_COUNT = 1000; // Number of recent codes to cache
        private const string RECENT_CODES_CACHE_KEY = "recent_discount_codes";
        private const string DISCOUNT_CODE_CACHE_KEY_PREFIX = "discountcode";

        public async Task<DiscountCode> GetByIdAsync(int id)
        {
            string key = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:{id}";
            try
            {
                DiscountCode? code = await cacheService.GetAsync<DiscountCode>(key);

                if (code == null)
                {
                    Log.Information("Cache miss for discount code ID {Id}", id);
                    code = await context.DiscountCodes.FindAsync(id);
                    if (code != null)
                    {
                        await cacheService.SetAsync(key, code, TimeSpan.FromMinutes(5));
                        Log.Information("Discount code ID {Id} loaded from DB and cached", id);
                    }
                    else
                    {
                        Log.Warning("Discount code with ID {Id} not found", id);
                        throw new KeyNotFoundException($"Discount code with ID '{id}' not found.");
                    }
                }
                else
                {
                    Log.Information("Cache hit for discount code ID {Id}", id);
                }

                return code;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving discount code by ID {Id}", id);
                throw;
            }
        }

        public async Task<DiscountCode> GetDiscountCodeByCodeAsync(string code)
        {
            string key = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:code:{code}";
            try
            {
                DiscountCode? cachedCode = await cacheService.GetAsync<DiscountCode>(key);

                if (cachedCode == null)
                {
                    Log.Information("Cache miss for discount code '{Code}'", code);
                    cachedCode = await context.DiscountCodes.FirstOrDefaultAsync(c => c.Code == code);
                    if (cachedCode != null)
                    {
                        await cacheService.SetAsync(key, cachedCode, TimeSpan.FromMinutes(5));
                        Log.Information("Discount code '{Code}' loaded from DB and cached", code);
                    }
                    else
                    {
                        Log.Warning("Discount code '{Code}' not found", code);
                        throw new KeyNotFoundException($"Discount code '{code}' not found.");
                    }
                }
                else
                {
                    Log.Information("Cache hit for discount code '{Code}'", code);
                }

                return cachedCode!;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving discount code by code '{Code}'", code);
                throw;
            }
        }

        public async Task AddRangeDiscountCodeAsync(IEnumerable<DiscountCode> codes)
        {
            try
            {
                foreach (var code in codes)
                {
                    code.CreatedAt = DateTime.UtcNow;
                }
                await context.DiscountCodes.AddRangeAsync(codes);
                await context.SaveChangesAsync();

                // Batch update: refresh the recent codes cache once
                await UpdateRecentDiscountCodeCacheAsync(null!);

                foreach (var code in codes)
                {
                    Log.Information("Added discount code '{Code}", code.Code);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding discount codes");
                throw;
            }
        }

        public async Task UpdateDiscountCodeAsync(DiscountCode code)
        {
            try
            {
                context.Entry(code).State = EntityState.Modified;
                await context.SaveChangesAsync();
                Log.Information("Updated discount code '{Code}')", code.Code);

                // Invalidate the individual cache
                string key = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:{code.Code}";
                await cacheService.RemoveAsync(key);
                string codeKey = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:code:{code.Code}";
                await cacheService.RemoveAsync(codeKey);
                Log.Information("Invalidated cache for discount code '{Code}'", code.Code);

                // Update the cache of recent codes
                    await UpdateRecentDiscountCodeCacheAsync(code);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating discount code '{Code}'", code.Code);
                throw;
            }
        }

        public async Task<List<DiscountCode>> GetRecentDiscountCodesAsync()
        {
            try
            {
                var recentCodes = await cacheService.GetAsync<List<DiscountCode>>(RECENT_CODES_CACHE_KEY);

                if (recentCodes == null)
                {
                    Log.Information("Cache miss for recent discount codes");
                    recentCodes = await context.DiscountCodes
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(RECENT_CODES_COUNT)
                        .ToListAsync();

                    await cacheService.SetAsync(RECENT_CODES_CACHE_KEY, recentCodes, TimeSpan.FromMinutes(10));
                    Log.Information("Recent discount codes loaded from DB and cached");
                }
                else
                {
                    Log.Information("Cache hit for recent discount codes");
                }

                return recentCodes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving recent discount codes");
                throw;
            }
        }

        public async Task DeleteDiscountCodeAsync(DiscountCode code)
        {
            try
            {
                context.DiscountCodes.Remove(code);
                await context.SaveChangesAsync();
                Log.Information("Deleted discount code '{Code}'", code.Code);

                // Invalidate the individual cache
                string key = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:{code.Code}";
                await cacheService.RemoveAsync(key);
                string codeKey = $"{DISCOUNT_CODE_CACHE_KEY_PREFIX}:code:{code.Code}";
                await cacheService.RemoveAsync(codeKey);
                Log.Information("Invalidated cache for deleted discount code '{Code}'", code.Code);

                // Update the cache of recent codes
                await UpdateRecentDiscountCodeCacheAsync(null!);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting discount code '{Code}'", code.Code);
                throw;
            }
        }

        private async Task UpdateRecentDiscountCodeCacheAsync(DiscountCode newCode)
        {
            try
            {
                var recentCodes = await cacheService.GetAsync<List<DiscountCode>>(RECENT_CODES_CACHE_KEY);

                if (recentCodes == null)
                {
                    recentCodes = await context.DiscountCodes
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(RECENT_CODES_COUNT)
                        .ToListAsync();
                }
                else
                {
                    if (newCode != null)
                    {
                        recentCodes.Insert(0, newCode);
                        if (recentCodes.Count > RECENT_CODES_COUNT)
                        {
                            recentCodes.RemoveAt(recentCodes.Count - 1);
                        }
                    }
                    else
                    {
                        // If no new code is provided, just refresh the list from the database
                        recentCodes = await context.DiscountCodes
                            .OrderByDescending(c => c.CreatedAt)
                            .Take(RECENT_CODES_COUNT)
                            .ToListAsync();
                    }
                }

                await cacheService.SetAsync(RECENT_CODES_CACHE_KEY, recentCodes, TimeSpan.FromMinutes(10));
                Log.Information("Updated recent discount codes cache");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating recent discount codes cache");
                throw;
            }
        }

        public async Task<List<string>> GetAllCodesAsync()
        {
            const string ALL_CODES_CACHE_KEY = "all_discount_codes";
            try
            {
                // Try to get all codes from cache
                var codes = await cacheService.GetAsync<List<string>>(ALL_CODES_CACHE_KEY);

                if (codes == null)
                {
                    Log.Information("Cache miss for all discount codes");
                    codes = await context.DiscountCodes
                        .Select(dc => dc.Code)
                        .ToListAsync();

                    await cacheService.SetAsync(ALL_CODES_CACHE_KEY, codes, TimeSpan.FromMinutes(10));
                    Log.Information("All discount codes loaded from DB and cached");
                }
                else
                {
                    Log.Information("Cache hit for all discount codes");
                }

                return codes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving all discount codes.");
                throw;
            }
        }

        public async Task<List<string>> GetMostRecentCodesAsync(int count = 10)
        {
            try
            {
                // Try to get the recent codes from cache
                var recentCodes = await cacheService.GetAsync<List<DiscountCode>>(RECENT_CODES_CACHE_KEY);

                if (recentCodes == null)
                {
                    Log.Information("Cache miss for recent discount codes");
                    recentCodes = await context.DiscountCodes
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(RECENT_CODES_COUNT)
                        .ToListAsync();

                    await cacheService.SetAsync(RECENT_CODES_CACHE_KEY, recentCodes, TimeSpan.FromMinutes(10));
                    Log.Information("Recent discount codes loaded from DB and cached");
                }
                else
                {
                    Log.Information("Cache hit for recent discount codes");
                }

                // Return only the most recent 'count' codes as strings
                return recentCodes
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(count)
                    .Select(c => c.Code)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving the most recent discount codes");
                throw;
            }
        }

        public async Task PreloadDiscountCodeCachesAsync()
        {
            // Preload recent codes
            var recentCodes = await context.DiscountCodes
                .OrderByDescending(c => c.CreatedAt)
                .Take(RECENT_CODES_COUNT)
                .ToListAsync();
            await cacheService.SetAsync(RECENT_CODES_CACHE_KEY, recentCodes, TimeSpan.FromMinutes(10));

            // Preload all codes
            var allCodes = await context.DiscountCodes
                .Select(dc => dc.Code)
                .ToListAsync();
            await cacheService.SetAsync("all_discount_codes", allCodes, TimeSpan.FromMinutes(10));
        }
    }
}