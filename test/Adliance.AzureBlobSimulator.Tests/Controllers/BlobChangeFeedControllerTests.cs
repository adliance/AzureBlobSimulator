using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class BlobChangeFeedControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "blobchangefeed_tests_storage")
{
    [Fact]
    public async Task ListBlobs_BlobChangeFeedContainer_ReturnsEmptyList()
    {
        // Arrange
        var blobChangeFeedContainerClient = BlobServiceClient.GetBlobContainerClient("$blobchangefeed");

        // Act
        var blobs = new List<BlobItem>();
        await foreach (var blob in blobChangeFeedContainerClient.GetBlobsAsync())
        {
            blobs.Add(blob);
        }

        // Assert
        Assert.Empty(blobs);
    }

    [Fact]
    public async Task GetContainerProperties_BlobChangeFeedContainer_ReturnsProperties()
    {
        // Arrange
        var blobChangeFeedContainerClient = BlobServiceClient.GetBlobContainerClient("$blobchangefeed");

        // Act
        var properties = await blobChangeFeedContainerClient.GetPropertiesAsync();

        // Assert
        Assert.NotNull(properties);
        Assert.NotNull(properties.Value);
    }

    [Fact]
    public async Task BlobChangeFeedContainer_ExistsCheck_ReturnsTrue()
    {
        // Arrange
        var blobChangeFeedContainerClient = BlobServiceClient.GetBlobContainerClient("$blobchangefeed");

        // Act
        var exists = await blobChangeFeedContainerClient.ExistsAsync();

        // Assert
        Assert.True(exists.Value);
    }
}
