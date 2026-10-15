using be.Services.Checkout;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Infrastructure;

public sealed class CommerceExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (CommerceException exception)
        {
            await Write(context, exception.Status, exception.Code, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Write(context, 409, "CHECKOUT_CHANGED", "The resource changed. Refresh before trying again.");
        }
    }
    private static Task Write(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = message, Instance = context.Request.Path,
            Extensions = { ["code"] = code, ["correlationId"] = context.Items[CorrelationIdMiddleware.ItemName]?.ToString() ?? context.TraceIdentifier }
        }, options: (System.Text.Json.JsonSerializerOptions?)null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
