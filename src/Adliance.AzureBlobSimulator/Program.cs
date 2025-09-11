using Adliance.AzureBlobSimulator.Attributes;
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
app.MapGet("/health/{container}", ([ContainerName] string container, ContainerService containerService) =>
{
    if (string.IsNullOrWhiteSpace(container)) return Results.BadRequest("Container name is required.");
    if (containerService.DoesContainerExist(container)) return Results.Ok("Container exists.");
    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});
app.Run();

namespace Adliance.AzureBlobSimulator
{
    // required for TestServer support
    // ReSharper disable once PartialTypeWithSinglePart
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class Program;
}
