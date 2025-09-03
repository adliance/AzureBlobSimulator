using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class BlobsController(ContainerService containerService) : ControllerBase
{
    [HttpGet("/{container}")]
    public IActionResult HandleGetRequest(string container, [FromQuery] string? comp, [FromQuery] string? restype)
    {
        if (restype == "container" && comp == "list") return ListBlobs(container);
        if (restype == "container" && comp == null) return GetContainerProperties(container);
        return BadRequest("Unsupported operation on GET /<container>.");
    }

    private IActionResult ListBlobs(string container)
    {
        if (!containerService.DoesContainerExist(container)) return NotFound($"Container \"{container}\" not found.");

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        var filePaths = containerService.GetFilePaths(container);

        var blobs = filePaths.Select(filePath =>
        {
            var fileInfo = new FileInfo(filePath);
            return new Blob
            {
                Name = fileInfo.Name,
                Properties = new BlobProperties
                {
                    LastModified = fileInfo.LastWriteTimeUtc.ToString("R"),
                    Etag = Guid.NewGuid().ToString(),
                    ContentLength = fileInfo.Length,
                    ContentType = contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType) ? contentType : "application/octet-stream",
                    BlobType = "BlockBlob"
                }
            };
        });

        var response = new BlobEnumerationResults
        {
            ServiceEndpoint = $"{Request.Scheme}://{Request.Host}/",
            ContainerName = container,
            Blobs = new BlobsCollection(blobs.ToList())
        };

        var responseXml = XmlSerializationService.SerializeToXml(response);

        Response.Headers["x-ms-version"] = Constants.MsVersion;
        return Content(responseXml, "application/xml");
    }

    private IActionResult GetContainerProperties(string container)
    {
        if (!containerService.DoesContainerExist(container)) return NotFound($"Container \"{container}\" not found.");

        Response.Headers["x-ms-version"] = Constants.MsVersion;
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
}

/*
[ApiController]
public class BlobsController(IContainerPathResolver containerPathResolver) : ControllerBase
{

    [HttpHead]
    public IActionResult ContainerExists(string containerName)
    {
        try
        {
            var containerPath = containerPathResolver.GetContainerPath(containerName);

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


    [HttpPut("{*blobName}")]
    public async Task<IActionResult> UploadBlob(string containerName, string blobName)
    {
        blobName = FixBlobNameThatContainsIdenticalDirectoryAndFileName(blobName);

        try
        {
            var containerPath = containerPathResolver.GetContainerPath(containerName);
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


    // I don't understand why, but the Azure storage explorer often sends requests with
    // filename.pdf/filename.pdf when uploading, instead of just filename.pdf
    private static string FixBlobNameThatContainsIdenticalDirectoryAndFileName(string blobName)
    {
        var parts = blobName.Split('/');
        if (parts.Length == 2 && parts[0]==parts[1]) return parts[1];
        return blobName;
    }
}
*/
