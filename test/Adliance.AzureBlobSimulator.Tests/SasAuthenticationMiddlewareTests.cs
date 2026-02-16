using System.Text;
using Adliance.AzureBlobSimulator.Middleware;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Tests;

public class SasAuthenticationMiddlewareTests
{
    private readonly SasValidatorService _validator;
    private readonly IOptions<StorageOptions> _options;
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
        _options = server.Services.GetRequiredService<IOptions<StorageOptions>>();
        _validator = new SasValidatorService(_options, null, env);
    }

    [Fact]
    public async Task Valid_Sas_Allows_GET_Request()
    {
        const string sp = "rl";
        const string sr = "b";
        var sig = GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.GetAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}");

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    [Fact]
    public async Task Valid_Sas_Allows_PUT_Request()
    {
        const string sp = "wc";
        const string sr = "b";
        var sig = GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.PutAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}", null);

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    [Fact]
    public async Task Valid_Sas_Allows_DELETE_Request()
    {
        const string sp = "d";
        const string sr = "b";
        var sig = GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.DeleteAsync(
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}");

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    [Fact]
    public async Task Valid_Sas_Allows_HEAD_Request()
    {
        const string sp = "r";
        const string sr = "b";
        var sig = GenerateSignature(sp, null, Se, null, Sv, sr);
        var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head,
            $"{BaseUrl}?sv={Sv}&sp={sp}&se={Se}&sr={sr}&sig={Uri.EscapeDataString(sig)}"));

        Assert.True(response.IsSuccessStatusCode, response.ReasonPhrase);
    }

    private string GenerateSignature(string sp,
        string? st,
        string se,
        string? spr,
        string sv,
        string sr, string? canonicalizedResource = null, string? accountKey = null)
    {
        const string canonicalizedResourceContainer = "/blob/testaccount/mycontainer"; // sr=c
        const string canonicalizedResourceBlob = "/blob/testaccount/mycontainer/myblob.txt"; // sr=b

        accountKey ??= _options.Value.Accounts.FirstOrDefault(a => a.Name.Equals("testaccount"))?.Key;
        Assert.NotNull(accountKey);

        switch (sr)
        {
            case "c":
                return _validator.GenerateSignature(sp, st, se, canonicalizedResource ?? canonicalizedResourceContainer, spr, sv, sr, Convert.FromBase64String(accountKey));
            case "b":
                return _validator.GenerateSignature(sp, st, se, canonicalizedResource ?? canonicalizedResourceBlob, spr, sv, sr, Convert.FromBase64String(accountKey));
            default:
                Assert.Fail("Invalid sr value.");
                break;
        }

        return "INVALID";
    }
}
