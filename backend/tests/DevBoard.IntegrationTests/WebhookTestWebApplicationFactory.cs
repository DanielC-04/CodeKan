using DevBoard.Application.Realtime;
using DevBoard.Infrastructure.Persistence;
using DevBoard.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevBoard.IntegrationTests;

public sealed class WebhookTestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string WebhookSecret = "integration-test-webhook-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbName = $"devboard-tests-{Guid.NewGuid()}";

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));

            services.RemoveAll<ITaskRealtimeNotifier>();
            services.AddSingleton<TestTaskRealtimeNotifier>();
            services.AddSingleton<ITaskRealtimeNotifier>(provider => provider.GetRequiredService<TestTaskRealtimeNotifier>());

            services.Configure<GitHubWebhookOptions>(options => options.WebhookSecret = WebhookSecret);
        });
    }

    public async Task ExecuteDbAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(dbContext);
    }

    public async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }

    public TestTaskRealtimeNotifier GetNotifier()
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TestTaskRealtimeNotifier>();
    }
}

public sealed class TestTaskRealtimeNotifier : ITaskRealtimeNotifier
{
    private readonly List<TaskUpdatedEvent> _events = [];
    private readonly Lock _lock = new();

    public Task NotifyTaskUpdatedAsync(TaskUpdatedEvent taskUpdatedEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _events.Add(taskUpdatedEvent);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<TaskUpdatedEvent> Snapshot()
    {
        lock (_lock)
        {
            return _events.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }
}
