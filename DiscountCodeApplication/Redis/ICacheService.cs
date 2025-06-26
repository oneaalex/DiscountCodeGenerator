﻿using System.Threading;

namespace DiscountCodeApplication.Redis
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
