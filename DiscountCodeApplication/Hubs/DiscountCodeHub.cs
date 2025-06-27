using DiscountCodeApplication.Services;
using DiscountCodeApplication.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DiscountCodeApplication.Hubs
{
    public class DiscountCodeHub : Hub
    {
        private readonly IDiscountCodeService _service;
        private readonly string _expectedSecret;

        public DiscountCodeHub(IDiscountCodeService service, IConfiguration config)
        {
            _service = service;

            // Fix for CS8601: Ensure null safety by using null-coalescing operator
            var signalRSection = config.GetSection("SignalR");
            _expectedSecret = signalRSection?["Secret"] ?? string.Empty;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var providedSecret = httpContext?.Request.Query["secret"].ToString();

            if (string.IsNullOrEmpty(providedSecret) || providedSecret != _expectedSecret)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized: Invalid secret.");
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

        public async Task<bool> GenerateCode(ushort count, byte length)
        {
            Log.Information("GenerateCode called by {ConnectionId} with count {Count} and length {Length}", Context.ConnectionId, count, length);

            // Enforce constraints
            var validationError = DiscountCodeValidation.ValidateGenerateCodeInput(count, length);
            if (validationError != null)
            {
                await Clients.Caller.SendAsync("Error", validationError);
                return false;
            }

            try
            {
                var result = await _service.GenerateAndAddCodesAsync(count, length);
                if (result)
                {
                    // For demonstration, send a placeholder code of 8 chars (since you want string code has 8 chars)
                    // In a real scenario, you may want to return the actual generated codes.
                    string code = new string('X', 8);
                    Log.Information("Generated code {Code} for {ConnectionId}", code, Context.ConnectionId);
                    await Clients.All.SendAsync("CodeGenerated", code);
                    return true;
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to generate codes.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating code for {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to generate code");
                return false;
            }
        }

        public async Task<byte> UseCode(string code)
        {
            Log.Information("UseCode called by {ConnectionId} for {Code}", Context.ConnectionId, code);

            // Enforce constraints
            var validationError = DiscountCodeValidation.ValidateUseCodeInput(code);
            if (validationError != null)
            {
                await Clients.Caller.SendAsync("Error", "Code must be 8 characters or fewer.");
                return (byte)UseCodeResultEnum.Failure;
            }

            try
            {
                var result = await _service.UseCodeAsync(code);
                if (result == (byte)UseCodeResultEnum.Success)
                {
                    Log.Information("Code {Code} used successfully by {ConnectionId}", code, Context.ConnectionId);
                    await Clients.All.SendAsync("CodeUsed", code);
                }
                else
                {
                    // Send specific errors back to caller
                    string errorMessage = GetErrorMessage((UseCodeResultEnum)result);
                    await Clients.Caller.SendAsync("Error", errorMessage);
                }
                return result; // Always return the result byte
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error using code {Code} for {ConnectionId}", code, Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "An internal error occurred");
                return (byte)UseCodeResultEnum.Exception;
            }
        }

        private string GetErrorMessage(UseCodeResultEnum result)
        {
            return result switch
            {
                UseCodeResultEnum.AlreadyUsed => "This code has already been used.",
                UseCodeResultEnum.Expired => "This code is expired.",
                UseCodeResultEnum.Inactive => "This code is inactive.",
                UseCodeResultEnum.Deleted => "This code has been deleted.",
                UseCodeResultEnum.Failure => "Code not found.",
                UseCodeResultEnum.Exception => "An unexpected error occurred.",
                _ => "Unknown error."
            };
        }

        public async Task<List<string>> GetCodes()
        {
            Log.Information("GetCodes called by {ConnectionId}", Context.ConnectionId);
            try
            {
                var codes = await _service.GetAllCodesAsync();
                Log.Information("Retrieved {Count} codes for {ConnectionId}", codes.Count, Context.ConnectionId);
                return codes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving codes for {ConnectionId}", Context.ConnectionId);
                throw; // Let the client handle the error
            }
        }

        public async Task<List<string>> GetRecentCodes(int count = 10)
        {
            Log.Information("GetRecentCodes called by {ConnectionId} with count {Count}", Context.ConnectionId, count);
            try
            {
                var codes = await _service.GetMostRecentCodesAsync(count);
                Log.Information("Retrieved {Count} recent codes for {ConnectionId}", codes.Count, Context.ConnectionId);
                return codes;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving recent codes for {ConnectionId}", Context.ConnectionId);
                throw; // Let the client handle the error
            }
        }
    }
}
