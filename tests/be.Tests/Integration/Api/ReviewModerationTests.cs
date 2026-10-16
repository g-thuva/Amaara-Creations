using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Domain.Reviews;
using be.Infrastructure.Persistence;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests;
public sealed class ReviewModerationTests
{
    [Theory]
    [InlineData(OrderStatus.Completed, 100, 0, true)]
    [InlineData(OrderStatus.Delivered, 100, 0, true)]
    [InlineData(OrderStatus.Confirmed, 100, 0, false)]
    [InlineData(OrderStatus.PendingPayment, 0, 0, false)]
    [InlineData(OrderStatus.Completed, 100, 100, false)]
    public async Task VerificationUsesFulfilledPaidOrderItemsAndPublicationRequiresModeration(OrderStatus status, int settled, int refunded, bool verified)
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = "a",
            Name = "Test customer",
            UserName = "private@example.test",
            Email = "private@example.test"
        };
        var product = new Product
        {
            Name = "Review fixture",
            Slug = "review-fixture",
            Stock = 10
        };
        var order = new Order
        {
            User = user,
            OrderNumber = "PRIVATE-ORDER",
            Status = status,
            Total = 100,
            OrderItems = [new OrderItem
            {
                Product = product,
                ProductNameSnapshot = product.Name,
                Quantity = 1,
                Price = 100,
                UnitPrice = 100,
                Subtotal = 100
            }

            ],
            Payments = [new Payment
            {
                Provider = "Fixture",
                Method = "Mock",
                Amount = 100,
                SettledAmount = settled,
                RefundedAmount = refunded,
                Status = PaymentStatus.Paid
            }

            ]
        };
        db.Add(order);
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "a");
        var response = await client.PostAsJsonAsync($"/api/v1/products/{product.Id}/reviews", new { rating = 5, comment = "Test review", verifiedPurchase = !verified, userId = "other", orderItemId = 999 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(verified, created.GetProperty("verifiedPurchase").GetBoolean());
        Assert.Equal("Pending", created.GetProperty("moderationStatus").GetString());
        var publicBefore = await client.GetFromJsonAsync<JsonElement>($"/api/v1/products/{product.Id}/reviews");
        Assert.Equal(0, publicBefore.GetProperty("totalCount").GetInt32());
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "admin");
        var id = created.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/admin/reviews/{id}/moderation", new { status = "Published", reason = "Test moderation", rowVersion = "" })).StatusCode);
        var publicJson = await client.GetStringAsync($"/api/v1/products/{product.Id}/reviews");
        Assert.Contains("Test review", publicJson);
        Assert.DoesNotContain("private@example.test", publicJson);
        Assert.DoesNotContain("PRIVATE-ORDER", publicJson);
        Assert.DoesNotContain("userId", publicJson);
        Assert.DoesNotContain("orderItemId", publicJson);
        db.ChangeTracker.Clear();
        var saved = await db.Reviews.SingleAsync();
        Assert.Equal(verified, saved.OrderItemId.HasValue);
    }

    [Fact]
    public async Task OwnerEditReturnsReviewToModerationAndOtherCustomersCannotEditOrDelete()
    {
        await using var factory = new CommerceApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = "a",
            Name = "Owner",
            UserName = "owner@example.test"
        };
        var product = new Product
        {
            Name = "Fixture",
            Slug = "fixture"
        };
        var review = new Review
        {
            User = user,
            Product = product,
            Rating = 5,
            Comment = "Published text",
            ModerationStatus = "Published"
        };
        db.Add(review);
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Add("X-Test-User", "b");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/v1/reviews/{review.Id}", new { rating = 1, comment = "Wrong owner" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/reviews/{review.Id}")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-User");
        client.DefaultRequestHeaders.Add("X-Test-User", "a");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/v1/reviews/{review.Id}", new { rating = 4, comment = "Edited text" })).StatusCode);
        Assert.DoesNotContain("Edited text", await client.GetStringAsync($"/api/v1/products/{product.Id}/reviews"));
        Assert.Contains("Edited text", await client.GetStringAsync("/api/v1/reviews/mine"));
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/reviews/{review.Id}")).StatusCode);
        Assert.Equal("[]", await client.GetStringAsync("/api/v1/reviews/mine"));
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync($"/api/v1/products/{product.Id}/reviews", new { rating = 5, comment = "Replacement text" })).StatusCode);
    }
}
