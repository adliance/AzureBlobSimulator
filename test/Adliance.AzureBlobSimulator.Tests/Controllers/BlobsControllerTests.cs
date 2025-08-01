using System.Text;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class BlobsControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "blobs_tests_storage")
{
    [Fact]
    public async Task UploadBlob_NewBlob_UploadsSuccessfully()
    {
        // Arrange
        const string containerName = "upload-test-container";
        const string blobName = "test-upload.txt";
        const string content = "Hello, this is a test blob upload!";

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            // Create container first
            await containerClient.CreateIfNotExistsAsync();

            // Act
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var response = await blobClient.UploadAsync(stream, overwrite: true);

            // Assert
            Assert.NotNull(response);

            // Verify blob exists on filesystem
            var containerPath = Path.Combine(TestStoragePath, containerName);
            var blobPath = Path.Combine(containerPath, blobName);
            Assert.True(File.Exists(blobPath));

            var fileContent = await File.ReadAllTextAsync(blobPath);
            Assert.Equal(content, fileContent);
        }
        finally
        {
            // Cleanup
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task DownloadBlob_ExistingBlob_DownloadsSuccessfully()
    {
        // Arrange
        const string containerName = "download-test-container";
        const string blobName = "test-download.txt";
        const string content = "Content to download via Azure SDK";

        // Create test file directly on filesystem
        var containerPath = Path.Combine(TestStoragePath, containerName);
        var blobPath = Path.Combine(containerPath, blobName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllTextAsync(blobPath, content);

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Act
            var response = await blobClient.DownloadContentAsync();

            // Assert
            var downloadedContent = response.Value.Content.ToString();
            Assert.Equal(content, downloadedContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task DownloadBlob_BinaryFile_DownloadsCorrectly()
    {
        // Arrange
        const string containerName = "binary-test-container";
        const string blobName = "test-binary.bin";
        var testBytes = "Binary content test"u8.ToArray().Concat(new byte[]
        {
            0,
            1,
            2,
            3,
            255,
            254,
            253
        }).ToArray();

        // Create test file directly on filesystem
        var containerPath = Path.Combine(TestStoragePath, containerName);
        var blobPath = Path.Combine(containerPath, blobName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllBytesAsync(blobPath, testBytes);

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Act
            var response = await blobClient.DownloadContentAsync();

            // Assert
            var downloadedBytes = response.Value.Content.ToArray();
            Assert.Equal(testBytes, downloadedBytes);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task DownloadBlob_WithSubdirectories_DownloadsCorrectly()
    {
        // Arrange
        const string containerName = "subdirectory-test-container";
        const string blobName = "folder/subfolder/test-file.json";
        const string content = "{\"message\": \"Hello from subdirectory\"}";

        // Create test file in subdirectory
        var containerPath = Path.Combine(TestStoragePath, containerName);
        var blobPath = Path.Combine(containerPath, blobName.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);
        await File.WriteAllTextAsync(blobPath, content);

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Act
            var response = await blobClient.DownloadContentAsync();

            // Assert
            var downloadedContent = response.Value.Content.ToString();
            Assert.Equal(content, downloadedContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task ListBlobs_EmptyContainer_ReturnsEmptyList()
    {
        // Arrange
        const string containerName = "empty-container";
        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

            // Act
            var blobs = new List<BlobItem>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            // Assert
            Assert.Empty(blobs);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task ListBlobs_ContainerWithBlobs_ReturnsAllBlobs()
    {
        // Arrange
        const string containerName = "list-blobs-container";
        var testFiles = new Dictionary<string, string>
        {
            ["file1.txt"] = "Content 1",
            ["folder/file2.txt"] = "Content 2",
            ["folder/subfolder/file3.json"] = "{\"test\": true}",
            ["root-file.md"] = "# Root file"
        };

        var containerPath = Path.Combine(TestStoragePath, containerName);
        Directory.CreateDirectory(containerPath);

        // Create test files
        foreach (var (fileName, fileContent) in testFiles)
        {
            var filePath = Path.Combine(containerPath, fileName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, fileContent);
        }

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

            // Act
            var blobs = new List<BlobItem>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            // Assert
            Assert.Equal(testFiles.Count, blobs.Count);

            foreach (var expectedFile in testFiles.Keys)
            {
                Assert.Contains(blobs, b => b.Name == expectedFile);
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task GetBlobProperties_ExistingBlob_ReturnsProperties()
    {
        // Arrange
        const string containerName = "properties-test-container";
        const string blobName = "properties-test.txt";
        const string content = "Test content for properties";

        var containerPath = Path.Combine(TestStoragePath, containerName);
        var blobPath = Path.Combine(containerPath, blobName);
        Directory.CreateDirectory(containerPath);
        await File.WriteAllTextAsync(blobPath, content);

        try
        {
            var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Act
            var properties = await blobClient.GetPropertiesAsync();

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(content.Length, properties.Value.ContentLength);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task ListBlobs_WithSubdirectories_ReturnsForwardSlashSeparators()
    {
        // Arrange
        const string containerName = "path-separator-test";
        var testFiles = new Dictionary<string, string>
        {
            ["root-file.txt"] = "Root file content",
            ["folder/file-in-folder.txt"] = "File in folder",
            ["folder/subfolder/deep-file.txt"] = "Deep file content",
            ["another-folder/another-file.json"] = "{\"test\": true}",
            ["very/deep/nested/structure/file.md"] = "# Deep nested file"
        };

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        
        try
        {
            await containerClient.CreateIfNotExistsAsync();

            // Upload all test files
            foreach (var (blobName, content) in testFiles)
            {
                var blobClient = containerClient.GetBlobClient(blobName);
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            // Act - List all blobs
            var blobs = new List<BlobItem>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            // Assert
            Assert.Equal(testFiles.Count, blobs.Count);

            foreach (var expectedFileName in testFiles.Keys)
            {
                var matchingBlob = blobs.FirstOrDefault(b => b.Name == expectedFileName);
                Assert.NotNull(matchingBlob);
                
                // Verify the blob name uses forward slashes
                Assert.Equal(expectedFileName, matchingBlob.Name);
                
                // Verify no backslashes are present in the name
                Assert.DoesNotContain('\\', matchingBlob.Name);
                
                // If the file contains subdirectories, verify forward slashes are used
                if (expectedFileName.Contains('/'))
                {
                    Assert.Contains('/', matchingBlob.Name);
                }
            }
        }
        finally
        {
            // Cleanup
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task UploadAndDownload_WithSubdirectoryPaths_WorksCorrectly()
    {
        // Arrange
        const string containerName = "upload-download-path-test";
        const string blobName = "folder/subfolder/test-file.txt";
        const string content = "Test content in subdirectory";

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            await containerClient.CreateIfNotExistsAsync();

            // Act - Upload with forward slashes in blob name
            using var uploadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(uploadStream, overwrite: true);

            // Act - Download the blob
            var downloadResponse = await blobClient.DownloadContentAsync();
            var downloadedContent = downloadResponse.Value.Content.ToString();

            // Act - List blobs to verify the name format
            var blobs = new List<BlobItem>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob);
            }

            // Assert
            Assert.Equal(content, downloadedContent);
            Assert.Single(blobs);
            Assert.Equal(blobName, blobs[0].Name); // Should maintain forward slashes
            Assert.DoesNotContain('\\', blobs[0].Name); // Should not contain backslashes
        }
        finally
        {
            // Cleanup
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }

    [Fact]
    public async Task GetBlobProperties_WithSubdirectoryPath_WorksCorrectly()
    {
        // Arrange
        const string containerName = "properties-path-test";
        const string blobName = "nested/folder/structure/properties-test.txt";
        const string content = "Test content for properties";

        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        try
        {
            await containerClient.CreateIfNotExistsAsync();

            // Upload the blob
            using var uploadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            await blobClient.UploadAsync(uploadStream, overwrite: true);

            // Act - Get blob properties using forward slash path
            var properties = await blobClient.GetPropertiesAsync();

            // Assert
            Assert.NotNull(properties);
            Assert.Equal(content.Length, properties.Value.ContentLength);
        }
        finally
        {
            // Cleanup
            var containerPath = Path.Combine(TestStoragePath, containerName);
            if (Directory.Exists(containerPath))
            {
                Directory.Delete(containerPath, true);
            }
        }
    }
}
