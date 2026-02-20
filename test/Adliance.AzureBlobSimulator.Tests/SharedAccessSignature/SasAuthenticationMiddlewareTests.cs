using System.Text;
using Adliance.AzureBlobSimulator.Middleware;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Tests.SharedAccessSignature;

public class SasAuthenticationMiddlewareTests
{
    private readonly SasHelper _sasHelper;
    private readonly HttpClient _client;
    private const string Sv = "2026-02-06";
    private const string Se = "2099-01-01T00:00:00Z";
    private const string BaseUrl = "/testaccount/mycontainer/myblob.txt";

    public SasAuthenticationMiddlewareTests()
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<StorageOptions>(options =>
                {
                    options.Accounts =
                    [
                        new StorageAccountOptions
                        {
                            Name = "testaccount",
                            Key = Convert.ToBase64String(
                                Encoding.UTF8.GetBytes("1234567890123456"))
                        }
                    ];
                });

                services.AddSingleton<SasValidatorService>();
            })
            .Configure(app =>
            {
                app.UseMiddleware<SasAuthenticationMiddleware>();

                app.Run(context =>
                {
                    context.Response.StatusCode = 200;
                    return Task.CompletedTask;
                });
            });

        var server = new TestServer(builder);
        _client = server.CreateClient();

        var env = new TestHostEnvironment();
        var options = server.Services.GetRequiredService<IOptions<StorageOptions>>();
        var validator = new SasValidatorService(options, null, env);
        _sasHelper = new SasHelper(options, validator);
    }

    /// <summary>
    /// Tests that a valid SAS token with read/list permissions ("rl") allows a GET request
    /// to a blob resource. Ensures that the SAS authentication middleware correctly validates
    /// read access for blob resources.
    /// </summary>
    [Fact]
    public async Task Valid_Sas_Allows_GET_Request()
    {
        const string sp = "rl";
        const string sr = "b";
        var sig = _sasHelper.GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.GetAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}");

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    /// <summary>
    /// Tests that a valid SAS token with write/create permissions ("wc") allows a PUT request
    /// to a blob resource. Ensures that the SAS authentication middleware correctly validates
    /// write access for blob resources.
    /// </summary>
    [Fact]
    public async Task Valid_Sas_Allows_PUT_Request()
    {
        const string sp = "wc";
        const string sr = "b";
        var sig = _sasHelper.GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.PutAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}", null);

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    /// <summary>
    /// Tests that a valid SAS token with delete permissions ("d") allows a DELETE request
    /// to a blob resource. Ensures that the SAS authentication middleware correctly validates
    /// delete access for blob resources.
    /// </summary>
    [Fact]
    public async Task Valid_Sas_Allows_DELETE_Request()
    {
        const string sp = "d";
        const string sr = "b";
        var sig = _sasHelper.GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.DeleteAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}");

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    /// <summary>
    /// Tests that a valid SAS token with read permissions ("r") allows a HEAD request
    /// to a blob resource. Ensures that the SAS authentication middleware correctly validates
    /// read metadata access for blob resources.
    /// </summary>
    [Fact]
    public async Task Valid_Sas_Allows_HEAD_Request()
    {
        const string sp = "r";
        const string sr = "b";
        var sig = _sasHelper.GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head,
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}"));

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }
}
