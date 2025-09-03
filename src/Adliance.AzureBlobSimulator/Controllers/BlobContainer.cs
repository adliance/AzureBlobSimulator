using System.Globalization;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Adliance.AzureBlobSimulator.Controllers;

public class BlobContainer(ContainerService containerService) : ControllerBase
{
    [HttpGet("/{container}/{*blob}")]
    public IActionResult ReadBlob(string container, string blob)
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
}
