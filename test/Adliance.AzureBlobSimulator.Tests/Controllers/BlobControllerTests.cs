using System.Text;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class BlobControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "blobs_tests_storage")
{
    [Fact]
    public async Task Can_Get_Blob()
    {
        const string containerName = "test-container";
        const string blobName = "my-little-test-blob_1.pdf";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllBytesAsync(Path.Combine(containerPath, blobName), "Hello World!"u8.ToArray());;

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync();
        Assert.NotNull(response?.Value);
        Assert.Equal("Hello World!", response.Value.Content.ToString());
    }

    [Fact]
    public async Task Cannot_Get_Blob_that_does_not_Exist()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test-blob");
        try
        {
            await blobClient.DownloadContentAsync();
            Assert.Fail("Should have thrown.");
        }
        catch
        {
            // OK, should fail
        }
    }
}
