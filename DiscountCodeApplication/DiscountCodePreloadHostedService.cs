using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using DiscountCodeApplication.Repository;

public class DiscountCodePreloadHostedService : IHostedService
{
    private readonly IDiscountCodeRepository _repository;

    public DiscountCodePreloadHostedService(IDiscountCodeRepository repository)
    {
        _repository = repository;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _repository.PreloadDiscountCodeCachesAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}