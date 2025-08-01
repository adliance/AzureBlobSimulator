using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class LogsControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "logs_tests_storage")
{
    [Fact]
    public async Task ListBlobs_LogsContainer_ReturnsEmptyList()
    {
        // Arrange
        var logsContainerClient = BlobServiceClient.GetBlobContainerClient("$logs");

        // Act
        var blobs = new List<BlobItem>();
        await foreach (var blob in logsContainerClient.GetBlobsAsync())
        {
            blobs.Add(blob);
        }

        // Assert
        Assert.Empty(blobs);
    }

    [Fact]
    public async Task GetContainerProperties_LogsContainer_ReturnsProperties()
    {
        // Arrange
        var logsContainerClient = BlobServiceClient.GetBlobContainerClient("$logs");

        // Act
        var properties = await logsContainerClient.GetPropertiesAsync();

        // Assert
        Assert.NotNull(properties);
        Assert.NotNull(properties.Value);
    }

    [Fact]
    public async Task LogsContainer_AppearsInContainerList()
    {
        // Act
        var containers = new List<BlobContainerItem>();
        await foreach (var container in BlobServiceClient.GetBlobContainersAsync())
        {
            containers.Add(container);
        }

        // Assert
        Assert.Contains(containers, c => c.Name == "$logs");
    }

    [Fact]
    public async Task LogsContainer_ExistsCheck_ReturnsTrue()
    {
        // Arrange
        var logsContainerClient = BlobServiceClient.GetBlobContainerClient("$logs");

        // Act
        var exists = await logsContainerClient.ExistsAsync();

        // Assert
        Assert.True(exists.Value);
    }
}
