using System.Globalization;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
[Route("/{containerName}")]
public class BlobsController(IContainerPathResolver containerPathResolver) : ControllerBase
{
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
    private readonly IContainerPathResolver _containerPathResolver = containerPathResolver;

    /// <summary>
    /// Converts a file system path to Azure blob name format (using forward slashes)
    /// </summary>
    private static string ConvertToAzureBlobName(string fileSystemPath)
    {
        return fileSystemPath.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Converts an Azure blob name to file system path (using OS-specific directory separator)
    /// </summary>
    private static string ConvertToFileSystemPath(string azureBlobName)
    {
        return azureBlobName.Replace('/', Path.DirectorySeparatorChar);
    }

    [HttpGet]
    public IActionResult HandleGetRequest(string containerName, [FromQuery] string? comp, [FromQuery] string? restype)
    {
        if (restype == "container" && comp == "list") return ListBlobs(containerName);
        if (restype == "container" && comp == null) return GetContainerProperties(containerName);

        var queryString = Request.QueryString.ToString();
        return BadRequest($"Invalid query parameters: comp={comp}, restype={restype}, query={queryString}");
    }

    private IActionResult GetContainerProperties(string containerName)
    {
        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);
            if (!Directory.Exists(containerPath) && containerName != "$logs" && containerName != "$blobchangefeed") return NotFound($"Container '{containerName}' not found");

            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";
            Response.Headers["x-ms-lease-status"] = "unlocked";
            Response.Headers["x-ms-lease-state"] = "available";
            Response.Headers["x-ms-has-immutability-policy"] = "false";
            Response.Headers["x-ms-has-legal-hold"] = "false";
            Response.Headers["x-ms-default-encryption-scope"] = "$account-encryption-key";
            Response.Headers["x-ms-deny-encryption-scope-override"] = "false";

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem($"Error getting container properties: {ex.Message}");
        }
    }

    [HttpPut]
    public IActionResult CreateContainer(string containerName, [FromQuery] string? restype)
    {
        if (restype != "container") return BadRequest("Missing required parameter: restype=container");

        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);

            if (Directory.Exists(containerPath))
            {
                // Container already exists - return 409 Conflict per Azure specification
                Response.Headers["x-ms-version"] = "2023-11-03";
                Response.Headers["x-ms-error-code"] = "ContainerAlreadyExists";

                var errorXml = """
                               <?xml version="1.0" encoding="utf-8"?>
                               <Error>
                                   <Code>ContainerAlreadyExists</Code>
                                   <Message>The specified container already exists.</Message>
                               </Error>
                               """;

                return new ContentResult
                {
                    StatusCode = 409,
                    Content = errorXml,
                    ContentType = "application/xml"
                };
            }

            Directory.CreateDirectory(containerPath);

            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";

            return StatusCode(201); // Created
        }
        catch (Exception ex)
        {
            return Problem($"Error creating container: {ex.Message}");
        }
    }

    [HttpHead]
    public IActionResult ContainerExists(string containerName)
    {
        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);

            if (!Directory.Exists(containerPath) && containerName != "$logs" && containerName != "$blobchangefeed")
            {
                return NotFound();
            }

            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["Last-Modified"] = DateTime.UtcNow.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";
            Response.Headers["x-ms-lease-status"] = "unlocked";
            Response.Headers["x-ms-lease-state"] = "available";

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem($"Error checking container existence: {ex.Message}");
        }
    }

    private IActionResult ListBlobs(string containerName)
    {
        try
        {
            // Handle special containers
            if (containerName == "$logs" || containerName == "$blobchangefeed")
            {
                return GetEmptyBlobList(containerName);
            }

            var containerPath = _containerPathResolver.GetContainerPath(containerName);

            if (!Directory.Exists(containerPath))
            {
                return NotFound($"Container '{containerName}' not found");
            }

            var blobs = Directory.GetFiles(containerPath, "*", SearchOption.AllDirectories)
                .Select(file =>
                {
                    var relativePath = Path.GetRelativePath(containerPath, file);
                    var fileInfo = new FileInfo(file);
                    return new Blob
                    {
                        Name = ConvertToAzureBlobName(relativePath),
                        Properties = new BlobProperties
                        {
                            LastModified = fileInfo.LastWriteTimeUtc.ToString("R"),
                            Etag = $"\"{Guid.NewGuid()}\"",
                            ContentLength = fileInfo.Length,
                            ContentType = _contentTypeProvider.TryGetContentType(file, out var contentType) ? contentType : "application/octet-stream",
                            BlobType = "BlockBlob"
                        }
                    };
                })
                .ToList();

            var response = new BlobEnumerationResults
            {
                ServiceEndpoint = $"{Request.Scheme}://{Request.Host}/",
                ContainerName = containerName,
                Blobs = new BlobsCollection
                {
                    Blob = blobs
                }
            };

            var responseXml = XmlSerializationService.SerializeToXml(response);

            Response.Headers["x-ms-version"] = "2023-11-03";
            return Content(responseXml, "application/xml");
        }
        catch (Exception ex)
        {
            return Problem($"Error listing blobs: {ex.Message}");
        }
    }

    [HttpGet("{*blobName}")]
    public IActionResult ReadBlob(string containerName, string blobName)
    {
        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);
            var blobPath = Path.Combine(containerPath, ConvertToFileSystemPath(blobName));

            if (!Directory.Exists(containerPath))
            {
                return NotFound($"Container '{containerName}' not found");
            }

            if (!System.IO.File.Exists(blobPath))
            {
                return NotFound($"Blob '{blobName}' not found in container '{containerName}'");
            }

            var fileInfo = new FileInfo(blobPath);
            var contentType = _contentTypeProvider.TryGetContentType(blobPath, out var detectedContentType) ? detectedContentType : "application/octet-stream";

            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["x-ms-blob-type"] = "BlockBlob";
            Response.Headers["x-ms-lease-status"] = "unlocked";
            Response.Headers["x-ms-lease-state"] = "available";
            Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";
            Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);

            var fileStream = new FileStream(blobPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(fileStream, contentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            return Problem($"Error reading blob: {ex.Message}");
        }
    }

    [HttpPut("{*blobName}")]
    public async Task<IActionResult> UploadBlob(string containerName, string blobName)
    {
        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);
            var blobPath = Path.Combine(containerPath, ConvertToFileSystemPath(blobName));

            // Create container if it doesn't exist
            Directory.CreateDirectory(containerPath);

            // Create directory for blob if needed
            var blobDirectory = Path.GetDirectoryName(blobPath);
            if (!string.IsNullOrEmpty(blobDirectory)) Directory.CreateDirectory(blobDirectory);

            // Write blob content
            await using var fileStream = new FileStream(blobPath, FileMode.Create, FileAccess.Write);
            await Request.Body.CopyToAsync(fileStream);

            var fileInfo = new FileInfo(blobPath);

            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["x-ms-blob-type"] = "BlockBlob";
            Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";
            Response.Headers["Content-MD5"] = "";
            Response.Headers["x-ms-request-server-encrypted"] = "false";

            return StatusCode(201); // Created
        }
        catch (Exception ex)
        {
            return Problem($"Error uploading blob: {ex.Message}");
        }
    }

    [HttpHead("{*blobName}")]
    public IActionResult GetBlobProperties(string containerName, string blobName)
    {
        try
        {
            var containerPath = _containerPathResolver.GetContainerPath(containerName);
            if (!Directory.Exists(containerPath)) return NotFound();

            var fileInfo = new FileInfo(Path.Combine(containerPath, ConvertToFileSystemPath(blobName)));
            if (!fileInfo.Exists) return NotFound();

            var contentType = _contentTypeProvider.TryGetContentType(fileInfo.FullName, out var detectedContentType) ? detectedContentType : "application/octet-stream";
            Response.Headers["x-ms-version"] = "2023-11-03";
            Response.Headers["x-ms-blob-type"] = "BlockBlob";
            Response.Headers["x-ms-lease-status"] = "unlocked";
            Response.Headers["x-ms-lease-state"] = "available";
            Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
            Response.Headers["ETag"] = $"\"{Guid.NewGuid()}\"";
            Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);
            Response.Headers["Content-Type"] = contentType;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["x-ms-server-encrypted"] = "false";

            return Ok();
        }
        catch (Exception ex)
        {
            return Problem($"Error getting blob properties: {ex.Message}");
        }
    }

    private IActionResult GetEmptyBlobList(string containerName)
    {
        var response = new BlobEnumerationResults
        {
            ServiceEndpoint = $"{Request.Scheme}://{Request.Host}/",
            ContainerName = containerName,
            Blobs = new BlobsCollection
            {
                Blob = new List<Blob>()
            }
        };

        Response.Headers["x-ms-version"] = "2023-11-03";
        return Content(XmlSerializationService.SerializeToXml(response), "application/xml");
    }
}
