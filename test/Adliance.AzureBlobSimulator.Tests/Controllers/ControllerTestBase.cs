using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public abstract class ControllerTestBase : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly BlobServiceClient BlobServiceClient;
    protected readonly string TestStoragePath;
    protected readonly string StorageAccountName;
    protected readonly string StorageAccountKey;

    protected ControllerTestBase(WebApplicationFactory<Program> factory, string testStorageSubdirectory)
    {
        TestStoragePath = Path.Combine("./unittests_storage", testStorageSubdirectory);
        StorageAccountName = "devstoreaccount1";
        StorageAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:LocalPath"] = TestStoragePath,
                    ["Storage:Accounts:0:Name"] = Guid.NewGuid().ToString(),
                    ["Storage:Accounts:0:Key"] = Guid.NewGuid().ToString(),
                    ["Storage:Accounts:1:Name"] = StorageAccountName,
                    ["Storage:Accounts:1:Key"] = StorageAccountKey
                    // Intentionally no containers configuration to avoid system directory creation
                });
            });
        });

        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions
        {
            Transport = new HttpClientTransport(httpClient)
        };

        var credential = new StorageSharedKeyCredential(StorageAccountName, StorageAccountKey);
        BlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, credential, options);

        Directory.CreateDirectory(TestStoragePath);
    }

    public virtual void Dispose()
    {
        if (Directory.Exists(TestStoragePath))
        {
            Directory.Delete(TestStoragePath, true);
        }
    }
}
