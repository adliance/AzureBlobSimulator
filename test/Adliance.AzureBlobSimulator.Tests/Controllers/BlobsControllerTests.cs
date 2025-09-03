using System.Text;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class BlobsControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory)
{
    [Fact]
    public async Task Can_Get_Container_Properties()
    {
        const string containerName = "test-container";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var response = await containerClient.GetPropertiesAsync();
        Assert.NotNull(response?.Value);
    }

    [Fact]
    public async Task Can_List_Blobs()
    {
        const string containerName = "test-container";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        var fileNames = new[]
        {
            "file1.txt",
            "file-2.pdf",
            "file__3.png"
        };

        foreach (var fileName in fileNames)
        {
            await File.WriteAllBytesAsync(Path.Combine(containerPath, fileName), Encoding.UTF8.GetBytes(fileName));
        }

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobs = new List<BlobItem>();

        await foreach (var b in containerClient.GetBlobsAsync()) blobs.Add(b);

        Assert.Equal(fileNames.Length, blobs.Count);
        foreach (var blob in blobs)
        {
            Assert.Contains(fileNames, f => f == blob.Name);
            Assert.Equal(blob.Name.Length, blob.Properties.ContentLength);
        }
    }

    [Fact]
    public async Task Cannot_Get_Container_Properties_of_Container_that_does_not_Exist()
    {
        const string containerName = "test-container";
        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        await CustomAssert.RequestError(404, () => containerClient.GetPropertiesAsync());
    }

    [Fact]
    public async Task Will_Get_404_when_Listing_Blobs_of_Container_that_does_not_Exist()
    {
        const string containerName = "test-container";
        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

        await CustomAssert.RequestError(404, async () =>
        {
            await foreach (var _ in containerClient.GetBlobsAsync())
            {
               // do nothing here
            }
        });
    }

    [Fact]
    public async Task Will_Get_400_on_any_Unsupported_Request()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("test-container");
        await CustomAssert.RequestError(400, () => containerClient.GetAccessPolicyAsync());
    }
}
