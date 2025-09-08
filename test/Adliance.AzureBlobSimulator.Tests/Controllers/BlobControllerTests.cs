using Azure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class BlobControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory)
{
    [Fact]
    public async Task Can_Upload_Blob()
    {
        const string containerName = "upload-test-container";
        const string blobName = "uploaded-file.txt";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var content = BinaryData.FromString("Hello Upload!");
        await blobClient.UploadAsync(content);

        var download = await blobClient.DownloadContentAsync();
        Assert.Equal("Hello Upload!", download.Value.Content.ToString());
        Assert.True(File.Exists(Path.Combine(containerPath, blobName)));
    }

    [Fact]
    public async Task Upload_To_NonExisting_Container_Should_Return_404()
    {
        const string containerName = "nonexisting-container";
        var blobClient = BlobServiceClient.GetBlobContainerClient(containerName).GetBlobClient("file.txt");
        await CustomAssert.RequestError(404, () => blobClient.UploadAsync(BinaryData.FromString("x")));
    }

    [Fact]
    public async Task Can_Get_Blob()
    {
        const string containerName = "test-container";
        const string blobName = "my-little-test-blob_1.pdf";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllBytesAsync(Path.Combine(containerPath, blobName), "Hello World!"u8.ToArray());

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync();
        Assert.NotNull(response?.Value);
        Assert.Equal("Hello World!", response.Value.Content.ToString());
    }

    [Fact]
    public async Task Can_Get_Properties_of_Blob()
    {
        const string containerName = "test-container";
        const string blobName = "my-little-test-blob_1.pdf";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllBytesAsync(Path.Combine(containerPath, blobName), "Hello World!"u8.ToArray());
        ;

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var response = await blobClient.GetPropertiesAsync();
        Assert.NotNull(response?.Value);
        Assert.Equal("Hello World!".Length, response.Value.ContentLength);
    }

    [Fact]
    public async Task Will_Get_404_when_Getting__Blob_that_does_not_Exist()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test-blob");
        await CustomAssert.RequestError(404, () => blobClient.DownloadContentAsync());
    }

    [Fact]
    public async Task Will_Get_404_when_Getting_Properties_of_Blob_that_does_not_Exist()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test-blob");
        await CustomAssert.RequestError(404, () => blobClient.GetPropertiesAsync());
    }

    [Fact]
    public async Task Will_Get_400_when_Getting_Blob_with_Invalid_Name()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test/-blob");
        await CustomAssert.RequestError(400, () => blobClient.GetPropertiesAsync());
    }

    [Fact]
    public async Task Will_Get_400_when_Getting_Blob_in_Container_with_Invalid_Name()
    {
        var containerClient = BlobServiceClient.GetBlobContainerClient("_testcontainer");
        var blobClient = containerClient.GetBlobClient("test-blob");
        await CustomAssert.RequestError(400, () => blobClient.GetPropertiesAsync());
    }
}
