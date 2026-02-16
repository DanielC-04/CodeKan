using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DevBoard.IntegrationTests;

public sealed class GitHubWebhooksControllerTests : IClassFixture<WebhookTestWebApplicationFactory>
{
    private readonly WebhookTestWebApplicationFactory _factory;

    public GitHubWebhooksControllerTests(WebhookTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Receive_WithIssuesClosed_UpdatesTaskToDoneAndEmitsEvent()
    {
        _factory.GetNotifier().Clear();
        var taskId = await SeedTaskAsync(issueNumber: 77, status: "InProgress", title: "Initial");

        using var client = _factory.CreateClient();
        const string deliveryId = "delivery-closed-001";
        var payload = BuildPayload("closed", 77, "Updated title");
        var request = BuildRequest(deliveryId, payload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persistedTask = await _factory.ExecuteDbAsync(db =>
            db.Tasks.AsNoTracking().FirstAsync(item => item.Id == taskId));

        Assert.Equal(DevBoard.Domain.Enums.TaskStatus.Done, persistedTask.Status);
        Assert.NotNull(persistedTask.CompletedAt);

        var notifier = _factory.GetNotifier();
        var events = notifier.Snapshot();

        Assert.Single(events);
        Assert.Equal(taskId, events[0].TaskId);
        Assert.Equal("Done", events[0].Status);
        Assert.Equal("webhook", events[0].UpdatedFrom);
    }

    [Fact]
    public async Task Receive_WithIssuesReopened_UpdatesTaskToInProgress()
    {
        _factory.GetNotifier().Clear();
        var taskId = await SeedTaskAsync(issueNumber: 78, status: "Done", title: "Done task");

        using var client = _factory.CreateClient();
        const string deliveryId = "delivery-reopened-001";
        var payload = BuildPayload("reopened", 78, "Done task");
        var request = BuildRequest(deliveryId, payload);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var persistedTask = await _factory.ExecuteDbAsync(db =>
            db.Tasks.AsNoTracking().FirstAsync(item => item.Id == taskId));

        Assert.Equal(DevBoard.Domain.Enums.TaskStatus.InProgress, persistedTask.Status);
        Assert.Null(persistedTask.CompletedAt);
    }

    [Fact]
    public async Task Receive_WithInvalidSignature_ReturnsUnauthorized()
    {
        _factory.GetNotifier().Clear();
        await SeedTaskAsync(issueNumber: 79, status: "InProgress", title: "Secured task");

        using var client = _factory.CreateClient();
        const string deliveryId = "delivery-invalid-signature";
        var payload = BuildPayload("closed", 79, "Secured task");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-GitHub-Event", "issues");
        request.Headers.Add("X-GitHub-Delivery", deliveryId);
        request.Headers.Add("X-Hub-Signature-256", "sha256=invalid");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var deliveriesCount = await _factory.ExecuteDbAsync(db => db.WebhookDeliveries.CountAsync());
        Assert.Equal(0, deliveriesCount);
    }

    [Fact]
    public async Task Receive_WithDuplicateDelivery_DoesNotReprocess()
    {
        _factory.GetNotifier().Clear();
        var taskId = await SeedTaskAsync(issueNumber: 80, status: "InProgress", title: "Duplicate task");

        using var client = _factory.CreateClient();
        const string deliveryId = "delivery-dup-001";
        var payload = BuildPayload("closed", 80, "Duplicate task");

        var firstResponse = await client.SendAsync(BuildRequest(deliveryId, payload));
        var secondResponse = await client.SendAsync(BuildRequest(deliveryId, payload));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var deliveriesCount = await _factory.ExecuteDbAsync(db => db.WebhookDeliveries.CountAsync());
        Assert.Equal(1, deliveriesCount);

        var persistedTask = await _factory.ExecuteDbAsync(db =>
            db.Tasks.AsNoTracking().FirstAsync(item => item.Id == taskId));

        Assert.Equal(DevBoard.Domain.Enums.TaskStatus.Done, persistedTask.Status);

        var notifier = _factory.GetNotifier();
        Assert.Single(notifier.Snapshot());
    }

    private async Task<Guid> SeedTaskAsync(int issueNumber, string status, string title)
    {
        return await _factory.ExecuteDbAsync(async dbContext =>
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var project = new DevBoard.Domain.Entities.Project("DevBoard", "carra", "devboard", "encrypted-token");
            var task = new DevBoard.Domain.Entities.Task(project.Id, title);
            task.SetGitHubIssueNumber(issueNumber);

            if (status == "InProgress")
            {
                task.MoveTo(DevBoard.Domain.Enums.TaskStatus.InProgress);
            }
            else if (status == "Done")
            {
                task.MoveTo(DevBoard.Domain.Enums.TaskStatus.Done);
            }

            dbContext.Projects.Add(project);
            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync();

            return task.Id;
        });
    }

    private static HttpRequestMessage BuildRequest(string deliveryId, string payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/github")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-GitHub-Event", "issues");
        request.Headers.Add("X-GitHub-Delivery", deliveryId);
        request.Headers.Add("X-Hub-Signature-256", BuildSignature(payload, WebhookTestWebApplicationFactory.WebhookSecret));

        return request;
    }

    private static string BuildPayload(string action, int issueNumber, string title)
    {
        var payload = new
        {
            action,
            issue = new
            {
                number = issueNumber,
                title
            },
            repository = new
            {
                name = "devboard",
                owner = new
                {
                    login = "carra"
                }
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string BuildSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
