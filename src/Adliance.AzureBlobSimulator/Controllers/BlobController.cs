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
        if (!string.Equals(blobType, "BlockBlob", StringComparison.OrdinalIgnoreCase)) return BadRequest("Only BlockBlob uploads are supported.");

        if (!containerService.DoesContainerExist(container)) return NotFound($"Container \"{container}\" not found.");

        var containerPath = containerService.GetContainerPath(container);
        var targetPath = Path.Combine(containerPath, blob);
        await using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
        await Request.Body.CopyToAsync(fs);
        await fs.FlushAsync();

        var fileInfo = new FileInfo(targetPath);
        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["ETag"] = Guid.NewGuid().ToString();
        Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
        Response.Headers["x-ms-request-server-encrypted"] = "false";
        return StatusCode(201);
    }

    [HttpGet("/{container}/{*blob}")]
    public IActionResult ReadBlob([FromRoute, ContainerName] string container, [FromRoute, BlobName] string blob)
    {
        if (!containerService.DoesBlobExist(container, blob)) return NotFound($"Blob \"{container}/{blob}\" not found.");

        var fileInfo = new FileInfo(containerService.GetBlobPath(container, blob));
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        var contentType = contentTypeProvider.TryGetContentType(fileInfo.Name, out var c) ? c : "application/octet-stream";

        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-blob-type"] = "BlockBlob";
        Response.Headers["x-ms-lease-status"] = "unlocked";
        Response.Headers["x-ms-lease-state"] = "available";
        Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
        Response.Headers["ETag"] = Guid.NewGuid().ToString();
        Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);

        var fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
        return File(fileStream, contentType, enableRangeProcessing: true);
    }

    [HttpHead("/{container}/{*blob}")]
    public IActionResult GetBlobProperties([FromRoute, ContainerName] string container, [FromRoute, BlobName] string blob)
    {
        if (!containerService.DoesBlobExist(container, blob)) return NotFound($"Blob \"{container}/{blob}\" not found.");

        var fileInfo = new FileInfo(containerService.GetBlobPath(container, blob));
        var contentTypeProvider = new FileExtensionContentTypeProvider();

        var contentType = contentTypeProvider.TryGetContentType(fileInfo.FullName, out var detectedContentType) ? detectedContentType : "application/octet-stream";
        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-blob-type"] = "BlockBlob";
        Response.Headers["x-ms-lease-status"] = "unlocked";
        Response.Headers["x-ms-lease-state"] = "available";
        Response.Headers["Last-Modified"] = fileInfo.LastWriteTimeUtc.ToString("R");
        Response.Headers["ETag"] = Guid.NewGuid().ToString();
        Response.Headers["Content-Length"] = fileInfo.Length.ToString(CultureInfo.InvariantCulture);
        Response.Headers["Content-Type"] = contentType;
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["x-ms-server-encrypted"] = "false";
        return Ok();
    }
}
