using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DiscountCodeApplication.Repository;

public class DiscountCodePreloadHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DiscountCodePreloadHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IDiscountCodeRepository>();
            await repository.PreloadDiscountCodeCachesAsync();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}