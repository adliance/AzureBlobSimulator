using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Adliance.AzureBlobSimulator.Controllers;

[ApiController]
public class VersionController() : ControllerBase
{
    [HttpGet("/version")]
    public IActionResult HandleGetRequest()
    {
        var assemblyVersion = Assembly.GetAssembly(typeof(Program))?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return Ok($"v{assemblyVersion}");
    }
}
