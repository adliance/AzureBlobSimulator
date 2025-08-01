using Adliance.AzureBlobSimulator.Middleware;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure Storage options
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

// Register container path resolver service
builder.Services.AddSingleton<IContainerPathResolver, ContainerPathResolver>();

var app = builder.Build();

// Get storage configuration and ensure storage directories exist
var storageOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>();

// Create base storage directory
Directory.CreateDirectory(storageOptions.Value.LocalPath);

// Create all configured container directories
foreach (var container in storageOptions.Value.Containers)
{
    Directory.CreateDirectory(container.LocalPath);
}

// Add Azure Storage authentication middleware
app.UseMiddleware<AzureStorageAuthenticationMiddleware>();

app.MapControllers();

app.Run();

namespace Adliance.AzureBlobSimulator
{
    // required for TestServer support
    public partial class Program
    {
    }
}
