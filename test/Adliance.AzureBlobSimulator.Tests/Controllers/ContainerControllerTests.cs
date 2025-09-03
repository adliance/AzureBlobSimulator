using Azure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Adliance.AzureBlobSimulator.Tests.Controllers;

public class ContainerControllerTests(WebApplicationFactory<Program> factory) : ControllerTestBase(factory, "containers_tests_storage")
{
    [Fact]
    public async Task Cannot_Create_Containers()
    {
        const string containerName = "new-test-container";
        var containerClient = BlobServiceClient.GetBlobContainerClient(containerName);

        try
        {
            await containerClient.CreateIfNotExistsAsync();
            Assert.Fail("Should have thrown.");
        }
        catch (RequestFailedException)
        {
            // OK, should fail
        }
    }
}
