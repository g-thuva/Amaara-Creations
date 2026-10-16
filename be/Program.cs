using be.Api.Extensions;
using be.Application;
using be.Infrastructure;
using be.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);
builder.AddAmaaraInfrastructure();
builder.Services.AddAmaaraApplication();
builder.AddAmaaraAuthentication();
builder.AddAmaaraApi();
var app = builder.Build();
app.UseAmaaraPipeline();
await app.BootstrapIdentityAsync();
app.Run();
public partial class Program
{
}
