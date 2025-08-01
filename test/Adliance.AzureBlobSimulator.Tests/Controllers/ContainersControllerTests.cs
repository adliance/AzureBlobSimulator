using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class ContainersControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "containers_tests_storage")
{
    [Fact]
    public async Task ListContainers_EmptyStorage_ReturnsSystemContainers()
    {
        // Act
        var containers = new List<BlobContainerItem>();
        await foreach (var container in BlobServiceClient.GetBlobContainersAsync()) containers.Add(container);

        // Assert - Should include system containers like $logs
        Assert.NotEmpty(containers);
        Assert.Contains(containers, c => c.Name == "$logs");
    }

    [Fact]
    public async Task ListContainers_WithUserContainers_ReturnsAllContainers()
    {
        // Arrange
        var testContainerNames = new[]
        {
            "container1",
            "container2",
            "test-container"
        };

        foreach (var containerName in testContainerNames)
        {
            var containerPath = Path.Combine(TestStoragePath, containerName);
            Directory.CreateDirectory(containerPath);
        }

        try
        {
            // Act
            var containers = new List<BlobContainerItem>();
            await foreach (var container in BlobServiceClient.GetBlobContainersAsync()) containers.Add(container);

            // Assert
            Assert.True(containers.Count >= testContainerNames.Length + 1); // +1 for $logs

            foreach (var expectedContainer in testContainerNames) Assert.Contains(containers, c => c.Name == expectedContainer);
            Assert.Contains(containers, c => c.Name == "$logs");
        }
        finally
        {
            // Cleanup
            foreach (var containerName in testContainerNames)
            {
                var containerPath = Path.Combine(TestStoragePath, containerName);
                if (Directory.Exists(containerPath)) Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task CreateContainer_NewContainer_CreatesSuccessfully()
    {
        // Arrange
        const string containerName = "new-test-container";
        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            // Act
            var response = await containerClient.CreateIfNotExistsAsync();

            // Assert
            Assert.NotNull(response);

            // Verify container exists on filesystem
            var containerPath = Path.Combine(TestStoragePath, containerName);
            Assert.True(Directory.Exists(containerPath));

            // Verify it appears in container list
            var containers = new List<BlobContainerItem>();
            await foreach (var container in BlobServiceClient.GetBlobContainersAsync()) containers.Add(container);
            Assert.Contains(containers, c => c.Name == containerName);
        }
        finally
        {
            // Cleanup
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath)) Directory.Delete(containerPath, true);
        }
    }

    [Fact]
    public async Task CreateContainer_ExistingContainer_DoesNotFail()
    {
        // Arrange
        const string containerName = "existing-container";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            // Act
            await containerClient.CreateIfNotExistsAsync();

            // Assert - Should not throw and container should still exist
            var containerExists = Directory.Exists(containerPath);
            Assert.True(containerExists);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath)) Directory.Delete(containerPath, true);
        }
    }

    [Fact]
    public async Task GetAccountInfo_ValidRequest_ReturnsAccountProperties()
    {
        // Act
        var accountInfo = await BlobServiceClient.GetAccountInfoAsync();

        // Assert
        Assert.NotNull(accountInfo);
        Assert.NotNull(accountInfo.Value);
    }
}
