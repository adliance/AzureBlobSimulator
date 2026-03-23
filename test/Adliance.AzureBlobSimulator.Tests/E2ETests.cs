using System.Net;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Adliance.AzureBlobSimulator.Tests;

public class BlobSimulatorE2ETests : IAsyncLifetime
{
    private IContainer _simulatorContainer;
    private string? _connectionString;
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "e2e-storage");
    private const string AccountName = "devstoreaccount1";
    private const string AccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
    private const string ContainerName = "demo";

    public BlobSimulatorE2ETests()
    {
        Directory.CreateDirectory(_storagePath);
    }

    public async Task InitializeAsync()
    {
        var image = new ImageFromDockerfileBuilder()
            .WithContextDirectory(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath)
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath)
            .WithDockerfile("dockerfile")
            .WithName("azureblobsimulator:e2etest")
            .WithReuse(false)
            .Build();

        await image.CreateAsync();

        _simulatorContainer = new ContainerBuilder(image)
            .WithBindMount(_storagePath, "/storage")
            .WithEnvironment("Storage__LocalPath", "/storage")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:80")
            .WithPortBinding(80, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r
                    .ForPort(80)
                    .ForPath("/health")
                    .ForStatusCode(HttpStatusCode.OK))
            )
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _simulatorContainer.StartAsync(cts.Token);
        var logs = await _simulatorContainer.GetLogsAsync(ct: cts.Token);
        Console.WriteLine(logs);

        var port = _simulatorContainer.GetMappedPublicPort(80);

        _connectionString =
            $"DefaultEndpointsProtocol=http;" +
            $"AccountName={AccountName};" +
            $"AccountKey={AccountKey};" +
            $"BlobEndpoint=http://localhost:{port}/{ContainerName};";
    }

    public async Task DisposeAsync()
    {
        await _simulatorContainer.StopAsync();
        await _simulatorContainer.DisposeAsync();

        var storagePath = Path.Combine(Path.GetTempPath(), "e2e-storage");
        if (Directory.Exists(storagePath))
            Directory.Delete(storagePath, true);
    }

    /// <summary>
    /// Performs a full end-to-end test of the Azure Blob Simulator running in a Linux container:
    /// 1. Verifies that the simulator container is running Linux.
    /// 2. Ensures a blob container exists or creates it if missing.
    /// 3. Uploads a text blob to the container.
    /// 4. Downloads the same blob and verifies its content matches the uploaded data.
    /// </summary>
    /// <remarks>
    /// This test validates that the simulator correctly handles fundamental blob storage operations
    /// in a Linux environment, including container creation, blob upload, and blob download.
    /// It also acts as a sanity check to ensure the test is running in a Linux container,
    /// which is required for true Linux E2E testing.
    /// </remarks>
    [Fact]
    public async Task E2E_UploadAndDownloadBlob_ShouldSucceed()
    {
        var client = new BlobServiceClient(_connectionString);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var execResult = await _simulatorContainer.ExecAsync(["uname", "-s"], ct: cts.Token);
        Assert.Equal("Linux", execResult.Stdout.Trim());

        var containerFolder = Path.Combine(_storagePath, ContainerName);
        if (!Directory.Exists(containerFolder))
        {
            Directory.CreateDirectory(containerFolder);
        }

        var container = client.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlockBlobClient("file.txt");
        const string txt = "This is a E2E test file which should be uploaded and downloaded by the simulator.";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(txt));
        await blob.UploadAsync(stream, cancellationToken: cts.Token);

        var result = await blob.DownloadContentAsync(cts.Token);
        Assert.Equal(txt, result.Value.Content.ToString());
    }

    /// <summary>
    /// Performs a full end-to-end test of the Azure Blob Simulator running in a Linux container:
    /// 1. Verifies that the simulator container is running Linux.
    /// 2. Ensures a blob container exists or creates it if missing.
    /// 3. Uploads a text blob to the container.
    /// 4. Downloads the same blob and verifies its content matches the uploaded data.
    /// </summary>
    /// <remarks>
    /// This test validates that the simulator correctly handles fundamental blob storage operations
    /// in a Linux environment, including container creation, blob upload, and blob download.
    /// It also acts as a sanity check to ensure the test is running in a Linux container,
    /// which is required for true Linux E2E testing.
    /// </remarks>
    [Fact]
    public async Task E2E_UploadAndDownloadBlobAsStream_ShouldSucceed()
    {
        var client = new BlobServiceClient(_connectionString);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var execResult = await _simulatorContainer.ExecAsync(["uname", "-s"], ct: cts.Token);
        Assert.Equal("Linux", execResult.Stdout.Trim());

        var containerFolder = Path.Combine(_storagePath, ContainerName);
        if (!Directory.Exists(containerFolder))
        {
            Directory.CreateDirectory(containerFolder);
        }

        var container = client.GetBlobContainerClient(ContainerName);
        var blob = container.GetBlockBlobClient("file.txt");
        const string txt = "This is a E2E test file which should be uploaded and downloaded by the simulator.";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(txt));
        await blob.UploadAsync(stream, cancellationToken: cts.Token);

        var resultStream = await blob.OpenReadAsync(cancellationToken: cts.Token);
        using var streamReader = new StreamReader(resultStream);
        var result = await streamReader.ReadToEndAsync(cancellationToken: cts.Token);
        Assert.Equal(txt, result);
    }
}
