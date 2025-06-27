﻿using Serilog;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscountCodeApplication.Redis
{
    public sealed class RedisCacheService(IConnectionMultiplexer redisConnection) : ICacheService
    {
        private readonly IDatabase _db = redisConnection.GetDatabase();

        // Configure JsonSerializerOptions for consistent serialization/deserialization
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() 
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve // Handle potential circular references if needed
        };

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                string? value = await _db.StringGetAsync(key);
                if (string.IsNullOrEmpty(value))
                {
                    Log.Information("Cache miss for key '{Key}'", key);
                    return default;
                }
                Log.Information("Cache hit for key '{Key}'", key);
                try
                {
                    return JsonSerializer.Deserialize<T>(value, _jsonSerializerOptions);
                }
                catch (Exception serEx)
                {
                    Log.Error(serEx, "Deserialization failed for key '{Key}'", key);
                    return default;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving key '{Key}' from Redis cache", key);
                throw;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
        {
            try
            {
                string serializedValue;
                try
                {
                    serializedValue = JsonSerializer.Serialize(value, _jsonSerializerOptions);
                }
                catch (Exception serEx)
                {
                    Log.Error(serEx, "Serialization failed for key '{Key}'", key);
                    throw;
                }
                await _db.StringSetAsync(key, serializedValue, expiry);
                Log.Information("Set cache for key '{Key}' with expiry {Expiry}", key, expiry);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error setting key '{Key}' in Redis cache", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                bool removed = await _db.KeyDeleteAsync(key);
                if (removed)
                {
                    Log.Information("Removed cache for key '{Key}'", key);
                }
                else
                {
                    Log.Warning("Attempted to remove key '{Key}' but it did not exist in cache", key);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error removing key '{Key}' from Redis cache", key);
                throw;
            }
        }
    }
}