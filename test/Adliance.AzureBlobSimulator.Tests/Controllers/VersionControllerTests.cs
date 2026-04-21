using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class VersionControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory)
{
    [Fact]
    public async Task GetVersionTest()
    {
        var client = Factory.CreateClient();
        client.BaseAddress = BlobServiceClient.Uri;
        var response = await client.GetAsync("/version");
        Assert.StartsWith("v", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
