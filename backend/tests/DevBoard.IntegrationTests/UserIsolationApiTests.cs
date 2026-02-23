using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DevBoard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DevBoard.IntegrationTests;

public sealed class UserIsolationApiTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UserIsolationApiTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjectById_WhenProjectBelongsToAnotherUser_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var ownerToken = await RegisterAndGetTokenAsync(client, "owner-project@test.local");
        var projectId = await CreateProjectAsync(client, ownerToken, "Owner Project");
        var anotherUserToken = await RegisterAndGetTokenAsync(client, "other-project@test.local");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/projects/{projectId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anotherUserToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskById_WhenTaskBelongsToAnotherUser_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        var ownerToken = await RegisterAndGetTokenAsync(client, "owner-task@test.local");
        var projectId = await CreateProjectAsync(client, ownerToken, "Owner Tasks");

        var taskId = await _factory.ExecuteDbAsync(async db =>
        {
            var task = new DevBoard.Domain.Entities.Task(projectId, "Private task");
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return task.Id;
        });

        var anotherUserToken = await RegisterAndGetTokenAsync(client, "other-task@test.local");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", anotherUserToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email)
    {
        var payload = new
        {
            email,
            password = "P@ssword123!"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponseEnvelope<AuthTokenData>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Invalid auth response payload.");

        return envelope.Data?.AccessToken
            ?? throw new InvalidOperationException("Access token is missing in auth response.");
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, string bearerToken, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/projects")
        {
            Content = JsonContent.Create(new
            {
                name,
                repoOwner = "owner",
                repoName = "repo",
                gitHubToken = "token"
            })
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponseEnvelope<ProjectData>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Invalid project response payload.");

        return envelope.Data?.Id
            ?? throw new InvalidOperationException("Project id is missing in project response.");
    }

    private sealed record ApiResponseEnvelope<T>(bool Success, T? Data, string Message);
    private sealed record AuthTokenData(string AccessToken);
    private sealed record ProjectData(Guid Id);
}

public sealed class ApiTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbName = $"devboard-api-tests-{Guid.NewGuid()}";

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
        });
    }

    public async Task<T> ExecuteDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }
}
