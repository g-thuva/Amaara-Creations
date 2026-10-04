using be.Data;
using be.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace be.Tests
{
    public class RefreshSessionServiceTests
    {
        [Fact]
        public void HashRefreshTokenDoesNotReturnRawToken()
        {
            var service = CreateService();
            var rawToken = service.GenerateRawRefreshToken();
            var hash = service.HashRefreshToken(rawToken);

            Assert.NotEqual(rawToken, hash);
            Assert.Equal(64, hash.Length);
            Assert.Equal(hash, service.HashRefreshToken(rawToken));
        }

        private static RefreshSessionService CreateService()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AmaaraModelOnly;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;
            var context = new ApplicationDbContext(options);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:RefreshTokenDays"] = "21"
            }).Build();

            return new RefreshSessionService(context, userManager: null!, configuration);
        }
    }
}
