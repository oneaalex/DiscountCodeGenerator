using StackExchange.Redis;

namespace DiscountCodeServer.Redis
{
    public static class RedisConnectionFactory
    {
        private static readonly Lazy<ConnectionMultiplexer> _connection = new(() =>
        {
            var config = new ConfigurationOptions
            {
                EndPoints = { "localhost:6379" },
                AbortOnConnectFail = false,
                ConnectTimeout = 1000,
                SyncTimeout = 1000
            };
            return ConnectionMultiplexer.Connect(config);
        });

        public static ConnectionMultiplexer Connection => _connection.Value;
    }
}