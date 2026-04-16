using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class ContainersControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory)
{
    [Fact]
    public async Task GetsContainerListSuccessfully()
    {
        var containerNames = new Dictionary<string, string>
        {
            //<ContainerName, ContainerFolderName>
            { "container1", "folder1" },
            { "container2", "folder2" },
            { "test-container", "test-folder" }
        };

        foreach (var containerPath in containerNames.Values.Select(folder => Path.Combine(TestStoragePath, folder)))
        {
            Directory.CreateDirectory(containerPath);
        }

        // create new factory to test different folder name and container name
        var customFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                //overwrite existing config for LocalPath to prevent autodetection
                var dict = new Dictionary<string, string?>
                {
                    ["Storage:LocalPath"] = Path.Combine(TestStoragePath, "empty")
                };

                var i = 0;
                foreach (var (name, folder) in containerNames)
                {
                    dict[$"Storage:Containers:{i}:Name"] = name;
                    dict[$"Storage:Containers:{i}:LocalPath"] = Path.GetFullPath(Path.Combine(TestStoragePath, folder));
                    i++;
                }

                config.AddInMemoryCollection(dict);
            });
        });

        Directory.CreateDirectory(Path.Combine(TestStoragePath, "empty"));

        var options = new Azure.Storage.Blobs.BlobClientOptions
        {
            Transport = new Azure.Core.Pipeline.HttpClientTransport(customFactory.CreateClient())
        };
        var client = new Azure.Storage.Blobs.BlobServiceClient(new Uri("http://localhost"), new Azure.Storage.StorageSharedKeyCredential(StorageAccountName, StorageAccountKey), options);

        var containers = new List<BlobContainerItem>();
        await foreach (var container in client.GetBlobContainersAsync())
        {
            containers.Add(container);
        }

        Assert.NotEmpty(containers);
        Assert.Equal(2 + containerNames.Count, containers.Count);
        Assert.Contains(containers, c => c.Name == "$logs");
        Assert.Contains(containers, c => c.Name == "$blobchangefeed");
        foreach (var (name, _) in containerNames)
        {
            Assert.Contains(containers, c => c.Name == name);
        }
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
        await CustomAssert.RequestError(400, () => BlobServiceClient.GetStatisticsAsync());
    }
}
