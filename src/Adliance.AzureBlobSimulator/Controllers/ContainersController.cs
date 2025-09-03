using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class ContainersController(IOptions<StorageOptions> options, ContainerService containerService) : ControllerBase
{
    [HttpGet("/")]
    public IActionResult HandleGetRequest([FromQuery] string comp, [FromQuery] string? restype)
    {
        if (comp == "properties" && restype == "account") return GetAccountProperties();
        if (comp == "list") return ListContainers();
        return BadRequest("Unsupported operation on GET /.");
    }

    private IActionResult GetAccountProperties()
    {
        Response.Headers["x-ms-version"] = Constants.MsVersion;
        Response.Headers["x-ms-sku-name"] = "Standard_LRS";
        Response.Headers["x-ms-account-kind"] = "StorageV2";
        Response.Headers["x-ms-is-hns-enabled"] = "false";
        return Ok();
    }

    private IActionResult ListContainers()
    {
        var containers = new List<Container>();

        foreach (var d in containerService.GetContainers())
        {
            containers.Add(new Container(d.Value));
        }
        
        var response = new ContainerEnumerationResults
        {
            ServiceEndpoint = $"{Request.Scheme}://{Request.Host}/",
            Containers = new ContainersCollection
            {
                Container = containers
            }
        };

        var responseXml = XmlSerializationService.SerializeToXml(response);

        Response.Headers["x-ms-version"] = Constants.MsVersion;
        return Content(responseXml, "application/xml");
    }
}
