using System.Globalization;
using System.Net;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class AuthenticationTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "auth_tests_storage")
{
    [Fact]
    public async Task Request_WithoutAuthentication_Returns403()
    {
        var httpClient = Factory.CreateClient();

        var response = await httpClient.GetAsync("/test-container?restype=container");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("AuthenticationFailed", response.Headers.GetValues("x-ms-error-code").FirstOrDefault());
    }

    [Fact]
    public async Task Request_WithInvalidSharedKeyCredentials_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential("invalidaccount", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzET50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            async () => await containerClient.CreateIfNotExistsAsync());

        Assert.Equal(403, exception.Status);
    }

    [Fact]
    public async Task Request_WithInvalidAccountName_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential("wrongaccount", StorageAccountKey);
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            async () => await containerClient.CreateIfNotExistsAsync());

        Assert.Equal(403, exception.Status);
    }

    [Fact]
    public async Task Request_WithInvalidAccountKey_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential(StorageAccountName, "aW52YWxpZGtleQ==");
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            async () => await containerClient.CreateIfNotExistsAsync());

        Assert.Equal(403, exception.Status);
    }

    [Fact]
    public async Task Request_WithExpiredSasToken_Returns403()
    {
        var httpClient = Factory.CreateClient();

        var expiredDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var sasUrl = $"/test-container?restype=container&sv=2023-11-03&se={expiredDate}&sp=r&sr=c&sig=invalidsignature";

        var response = await httpClient.GetAsync(sasUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("AuthenticationFailed", response.Headers.GetValues("x-ms-error-code").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SAS token validation failed", content);
    }

    [Fact]
    public async Task Request_WithInvalidSasSignature_Returns403()
    {
        var httpClient = Factory.CreateClient();

        var futureDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var sasUrl = $"/test-container?restype=container&sv=2023-11-03&se={futureDate}&sp=r&sr=c&sig=invalidsignature";

        var response = await httpClient.GetAsync(sasUrl);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("AuthenticationFailed", response.Headers.GetValues("x-ms-error-code").FirstOrDefault());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SAS token validation failed", content);
    }

    [Fact]
    public async Task Request_WithMalformedAuthorizationHeader_Returns403()
    {
        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "InvalidFormat");

        var response = await httpClient.GetAsync("/test-container?restype=container");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("AuthenticationFailed", response.Headers.GetValues("x-ms-error-code").FirstOrDefault());
    }

    [Fact]
    public async Task Request_WithSharedKeyButMissingColon_Returns403()
    {
        var httpClient = Factory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "SharedKey invalidformat");

        var response = await httpClient.GetAsync("/test-container?restype=container");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("AuthenticationFailed", response.Headers.GetValues("x-ms-error-code").FirstOrDefault());
    }

    [Fact]
    public async Task Request_WithValidAuthentication_ReturnsSuccess()
    {
        const string containerName = "auth-success-test";
        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            var response = await containerClient.CreateIfNotExistsAsync();

            Assert.NotNull(response);
        }
        finally
        {
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task BlobUpload_WithInvalidAuth_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential("invalidaccount", "aW52YWxpZGtleQ==");
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test-blob.txt");

        using var stream = new MemoryStream("test content"u8.ToArray());

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            async () => await blobClient.UploadAsync(stream));

        Assert.Equal(403, exception.Status);
    }

    [Fact]
    public async Task BlobDownload_WithInvalidAuth_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential("invalidaccount", "aW52YWxpZGtleQ==");
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");
        var blobClient = containerClient.GetBlobClient("test-blob.txt");

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(
            blobClient.DownloadContentAsync);

        Assert.Equal(403, exception.Status);
    }

    [Fact]
    public async Task ListBlobs_WithInvalidAuth_Returns403()
    {
        var httpClient = Factory.CreateClient();
        var options = new BlobClientOptions();
        options.Transport = new HttpClientTransport(httpClient);

        var invalidCredential = new StorageSharedKeyCredential("invalidaccount", "aW52YWxpZGtleQ==");
        var invalidBlobServiceClient = new BlobServiceClient(httpClient.BaseAddress!, invalidCredential, options);

        var containerClient = invalidBlobServiceClient.GetBlobContainerClient("test-container");

        var exception = await Assert.ThrowsAsync<Azure.RequestFailedException>(async () =>
        {
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                break;
            }
        });

        Assert.Equal(403, exception.Status);
    }
}
