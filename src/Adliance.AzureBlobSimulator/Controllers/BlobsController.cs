using Adliance.AzureBlobSimulator.Attributes;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class BlobsController(ContainerService containerService) : ControllerBase
{
    [HttpGet("/{container}")]
    public IActionResult HandleGetRequest([FromRoute, ContainerName] string container, [FromQuery] string? comp, [FromQuery] string? restype)
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
