using Adliance.AzureBlobSimulator.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class ContainerController : ControllerBase
{
    [HttpPut("/{container}")]
    public IActionResult CreateContainer([FromRoute, ContainerName] string container)
    {
        return BadRequest("Unsupported operation on PUT /<container>.");
    }
}
