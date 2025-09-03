using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class ContainersControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "containers_tests_storage")
{
    [Fact]
    public async Task Can_Get_List_of_Containers()
    {
        var containerNames = new[]
        {
            "container1",
            "container2",
            "test-container"
        };

        foreach (var containerName in containerNames)
        {
            var containerPath = Path.Combine(TestStoragePath, containerName);
            Directory.CreateDirectory(containerPath);
        }

        var containers = new List<BlobContainerItem>();
        await foreach (var container in BlobServiceClient.GetBlobContainersAsync()) containers.Add(container);

        Assert.NotEmpty(containers);
        Assert.Equal(2 + containerNames.Length, containers.Count);
        Assert.Contains(containers, c => c.Name == "$logs");
        Assert.Contains(containers, c => c.Name == "$blobchangefeed");
        foreach (var containerName in containerNames) Assert.Contains(containers, c => c.Name == containerName);
    }

    [Fact]
    public async Task Can_Get_Get_AccountInfo()
    {
        var accountInfo = await BlobServiceClient.GetAccountInfoAsync();
        Assert.NotNull(accountInfo?.Value);
    }

    [Fact]
    public async Task Will_Get_400_on_any_Unsupported_Request()
    {
        try
        {
            await BlobServiceClient.GetStatisticsAsync();
            Assert.Fail("Should have thrown.");
        }
        catch (RequestFailedException ex)
        {
            Assert.Equal(400, ex.Status);
        }
    }
}
