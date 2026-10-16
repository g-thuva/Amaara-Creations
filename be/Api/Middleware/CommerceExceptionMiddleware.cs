using be.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Api.Middleware;
public sealed class CommerceExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (be.Application.Common.RequestValidationException exception)
        {
            context.Response.StatusCode = 400;
            var problem = new ValidationProblemDetails(exception.Errors.ToDictionary(p => p.Key, p => p.Value))
            {
                Status = 400,
                Title = "Validation failed",
                Instance = context.Request.Path
            };
            problem.Extensions["code"] = "VALIDATION_FAILED";
            problem.Extensions["correlationId"] = context.Items[CorrelationIdMiddleware.ItemName]?.ToString() ?? context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions? )null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
        catch (CommerceException exception)
        {
            await Write(context, exception.Status, exception.Code, exception.Message, exception.Details);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Write(context, 409, "CHECKOUT_CHANGED", "The resource changed. Refresh before trying again.");
        }
    }

    private static Task Write(HttpContext context, int status, string code, string message, IReadOnlyDictionary<string, object?>? details = null)
    {
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = message,
            Instance = context.Request.Path,
            Extensions =
            {
                ["code"] = code,
                ["message"] = message,
                ["correlationId"] = context.Items[CorrelationIdMiddleware.ItemName]?.ToString() ?? context.TraceIdentifier
            }
        };
        if (details != null)
            foreach (var entry in details)
                problem.Extensions[entry.Key] = entry.Value;
        return context.Response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions? )null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
