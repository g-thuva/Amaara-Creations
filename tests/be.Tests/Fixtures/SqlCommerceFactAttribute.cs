using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Domain.Cart;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Payments;
using be.Domain.Promotions;
using be.Domain.Shipping;
using be.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace be.Tests;
public sealed class SqlCommerceFactAttribute : FactAttribute
{
    public SqlCommerceFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION")))
            Skip = "Set AMAARA_TEST_SQL_CONNECTION for isolated real SQL Server concurrency verification.";
    }
}
