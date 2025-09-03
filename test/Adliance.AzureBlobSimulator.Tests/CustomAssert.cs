using Azure;

namespace Adliance.AzureBlobSimulator.Tests;

public static class CustomAssert
{
    public static async Task RequestError(int statusCode, Func<Task> action)
    {
        try
        {
            await action.Invoke();
            Assert.Fail($"Should have thrown a RequestFailedException with status code {statusCode}.");
        }
        catch (RequestFailedException ex)
        {
            Assert.Equal(statusCode, ex.Status);
        }
    }
}
