using be.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests
{
    public class CorrelationIdMiddlewareTests
    {
        [Fact]
        public async Task MiddlewarePreservesSafeIncomingCorrelationId()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "phase1-test-123";

            var middleware = new CorrelationIdMiddleware(
                next: ctx => Task.CompletedTask,
                logger: NullLogger<CorrelationIdMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            Assert.Equal("phase1-test-123", context.Items[CorrelationIdMiddleware.ItemName]);
        }
    }
}
