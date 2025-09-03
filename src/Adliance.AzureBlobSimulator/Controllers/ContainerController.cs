using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Mvc;

namespace Adliance.AzureBlobSimulator.Controllers;

public class ContainerController(ContainerService containerService) : ControllerBase
{
    [HttpPut("/{container}")]
    public IActionResult CreateContainer(string container)
    {
        return BadRequest("Unsupported operation on PUT /<container>.");
    }
}
