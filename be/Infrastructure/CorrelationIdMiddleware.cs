using System.Text.RegularExpressions;

namespace be.Infrastructure
{
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID";
        public const string ItemName = "CorrelationId";

        private static readonly Regex SafeCorrelationId = new("^[a-zA-Z0-9_.:-]{8,100}$", RegexOptions.Compiled);

        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestedId = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = !string.IsNullOrWhiteSpace(requestedId) && SafeCorrelationId.IsMatch(requestedId)
                ? requestedId
                : Guid.NewGuid().ToString("N");

            context.Items[ItemName] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }
    }
}
