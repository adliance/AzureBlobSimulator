using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
[Route("/")]
public class ContainersController(IOptions<StorageOptions> storageOptions, IContainerPathResolver containerPathResolver) : ControllerBase
{
    private readonly string _localStoragePath = storageOptions.Value.LocalPath;
    private readonly IContainerPathResolver _containerPathResolver = containerPathResolver;

    [HttpGet]
    public IActionResult HandleGetRequest([FromQuery] string comp, [FromQuery] string? restype)
    {
        if (comp == "properties" && restype == "account")
        {
            return GetAccountProperties();
        }

        if (comp == "list")

        {
            return ListContainers();
        }

        return BadRequest("Invalid query parameters");
    }

    private IActionResult GetAccountProperties()
    {
        Response.Headers["x-ms-version"] = "2023-11-03";
        Response.Headers["x-ms-sku-name"] = "Standard_LRS";
        Response.Headers["x-ms-account-kind"] = "StorageV2";
        Response.Headers["x-ms-is-hns-enabled"] = "false";

        return Ok();
    }

    private IActionResult ListContainers()
    {

        try
        {
            var containers = new List<Container>();

            // Add containers from base directory
            if (Directory.Exists(_localStoragePath))
            {
                var baseContainers = Directory.GetDirectories(_localStoragePath)
                    .Select(dir => new Container
                    {
                        Name = Path.GetFileName(dir),
                        Properties = new ContainerProperties
                        {
                            LastModified = Directory.GetLastWriteTimeUtc(dir).ToString("R"),
                            Etag = $"\"{Guid.NewGuid()}\"",
                            LeaseStatus = "unlocked",
                            LeaseState = "available"
                        }
                    });
                containers.AddRange(baseContainers);
            }

            // Add specifically configured containers
            foreach (var containerName in _containerPathResolver.GetConfiguredContainers())
            {
                var containerPath = _containerPathResolver.GetContainerPath(containerName);
                if (Directory.Exists(containerPath) && !containers.Any(c => c.Name == containerName))
                {
                    containers.Add(new Container
                    {
                        Name = containerName,
                        Properties = new ContainerProperties
                        {
                            LastModified = Directory.GetLastWriteTimeUtc(containerPath).ToString("R"),
                            Etag = $"\"{Guid.NewGuid()}\"",
                            LeaseStatus = "unlocked",
                            LeaseState = "available"
                        }
                    });
                }
            }

            // Always include the special $logs container
            containers.Add(new Container
            {
                Name = "$logs",
                Properties = new ContainerProperties
                {
                    LastModified = DateTime.UtcNow.ToString("R"),
                    Etag = $"\"{Guid.NewGuid()}\"",
                    LeaseStatus = "unlocked",
                    LeaseState = "available"
                }
            });

            // Always include the special $blobchangefeed container
            containers.Add(new Container
            {
                Name = "$blobchangefeed",
                Properties = new ContainerProperties
                {
                    LastModified = DateTime.UtcNow.ToString("R"),
                    Etag = $"\"{Guid.NewGuid()}\"",
                    LeaseStatus = "unlocked",
                    LeaseState = "available"
                }
            });

            var response = new ContainerEnumerationResults
            {
                ServiceEndpoint = $"{Request.Scheme}://{Request.Host}/",
                Containers = new ContainersCollection { Container = containers }
            };

            var responseXml = XmlSerializationService.SerializeToXml(response);

            Response.Headers["x-ms-version"] = "2023-11-03";
            return Content(responseXml, "application/xml");
        }
        catch (Exception ex)
        {
            return Problem($"Error listing containers: {ex.Message}");
        }
    }
}
