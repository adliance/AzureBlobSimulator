using System.Globalization;
using Adliance.AzureBlobSimulator.Attributes;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class BlobController(ContainerService containerService) : ControllerBase
{
    [HttpPut("/{container}/{*blob}")]
    public async Task<IActionResult> WriteBlob([FromRoute, ContainerName] string container, [FromRoute, BlobName] string blob)
    {
        var blobType = Request.Headers["x-ms-blob-type"].ToString();
        if (!string.Equals(blobType, "BlockBlob", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only BlockBlob uploads are supported.");
        }

        if (!containerService.DoesContainerExist(container))
        {
            return NotFound($"Container \"{container}\" not found.");
        }

        var containerPath = containerService.GetContainerPath(container);
        var targetPath = Path.Combine(containerPath, blob);

        if (!targetPath.StartsWith(containerPath, StringComparison.Ordinal))
        {
            return BadRequest("Invalid blob path.");
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
        await Request.Body.CopyToAsync(fs);
        await fs.FlushAsync();

        var fileInfo = new FileInfo(targetPath);
        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-request-server-encrypted"] = "false";
        Response.Headers.ETag = containerService.GetBlobETag(container, blob);
        Response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");
        return StatusCode(201);
    }

    [HttpGet("/{container}/{*blob}")]
    public IActionResult ReadBlob([FromRoute, ContainerName] string container, [FromRoute, BlobName] string blob)
    {
        var path = containerService.GetBlobPath(container, blob);

        if (!System.IO.File.Exists(path))
        {
            return NotFound($"Blob \"{container}/{blob}\" not found.");
        }

        var fileInfo = new FileInfo(path);
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        var contentType = contentTypeProvider.TryGetContentType(fileInfo.Name, out var c) ? c : "application/octet-stream";

        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-blob-type"] = "BlockBlob";
        Response.Headers["x-ms-lease-status"] = "unlocked";
        Response.Headers["x-ms-lease-state"] = "available";
        Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);
        Response.Headers.ETag = containerService.GetBlobETag(container, blob);
        Response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");

        // // support custom ms range header, see https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob?tabs=microsoft-entra-id#request
        // if (Request.Headers.TryGetValue("x-ms-range", out var requestedRange))
        //     Request.Headers.Range = requestedRange;

        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, contentType, enableRangeProcessing: true);
    }

    [HttpHead("/{container}/{*blob}")]
    public IActionResult GetBlobProperties([FromRoute, ContainerName] string container, [FromRoute, BlobName] string blob)
    {
        var path = containerService.GetBlobPath(container, blob);

        if (!System.IO.File.Exists(path))
        {
            return NotFound($"Blob \"{container}/{blob}\" not found.");
        }

        var fileInfo = new FileInfo(path);
        var contentTypeProvider = new FileExtensionContentTypeProvider();

        var contentType = contentTypeProvider.TryGetContentType(fileInfo.FullName, out var detectedContentType) ? detectedContentType : "application/octet-stream";
        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-blob-type"] = "BlockBlob";
        Response.Headers["x-ms-lease-status"] = "unlocked";
        Response.Headers["x-ms-lease-state"] = "available";
        Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);
        Response.Headers["x-ms-server-encrypted"] = "false";
        Response.Headers.AcceptRanges = "bytes";
        Response.Headers.ContentType = contentType;
        Response.Headers.ETag = containerService.GetBlobETag(container, blob);
        Response.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");
        return Ok();
    }

    [HttpDelete("/{container}/{*blob}")]
    public IActionResult DeleteBlob(
        [FromRoute, ContainerName] string container,
        [FromRoute, BlobName] string blob)
    {
        var path = containerService.GetBlobPath(container, blob);

        if (!System.IO.File.Exists(path))
        {
            return NotFound($"Blob \"{container}/{blob}\" not found.");
        }

        System.IO.File.Delete(path);

        containerService.CleanupBlobDirectoryChain(path, containerService.GetContainerPath(container));

        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-delete-type-permanent"] = "true";
        Response.Headers["x-ms-request-server-encrypted"] = "false";

        return Accepted();
    }
}
