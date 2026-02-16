using System.Text.Json;
using DevBoard.Application.Common;
using DevBoard.Application.Common.Exceptions;
using DevBoard.Domain.Exceptions;
using FluentValidation;

namespace DevBoard.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            var message = string.Join("; ", exception.Errors.Select(error => error.ErrorMessage).Distinct());
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, message);
        }
        catch (DomainException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (InvalidWebhookSignatureException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (GitHubIntegrationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status502BadGateway, exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled server exception.");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse<object>.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
