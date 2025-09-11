using Adliance.AzureBlobSimulator.Middleware;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddSingleton<ContainerService>();
builder.Services.AddTransient<SharedKeyAuthService>();

// Controllers and health checks
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseMiddleware<AzureStorageAuthenticationMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

namespace Adliance.AzureBlobSimulator
{
    // required for TestServer support
    // ReSharper disable once PartialTypeWithSinglePart
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Program;
}
