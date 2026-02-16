using DevBoard.Application.Common;
using DevBoard.Application.Webhooks.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBoard.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/github")]
public sealed class GitHubWebhooksController(IGitHubWebhookService gitHubWebhookService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<object>>> Receive(CancellationToken cancellationToken)
    {
        var eventName = Request.Headers["X-GitHub-Event"].ToString();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].ToString();
        var signature = Request.Headers["X-Hub-Signature-256"].ToString();

        if (string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(deliveryId))
        {
            return BadRequest(ApiResponse<object>.Fail("Missing required GitHub webhook headers."));
        }

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        await gitHubWebhookService.ProcessAsync(eventName, deliveryId, signature, payload, cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { }, "Webhook processed successfully."));
    }
}
