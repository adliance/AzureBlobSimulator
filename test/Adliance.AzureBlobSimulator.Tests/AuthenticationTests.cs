using System.Net;
using Azure;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Adliance.AzureBlobSimulator.Tests;

public class AuthenticationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void Will_Throw_when_no_Accounts_Configured()
    {
        var f = factory.WithWebHostBuilder(builder => { builder.ConfigureAppConfiguration((_, config) => { config.Sources.Clear(); }); });

        Assert.Throws<Exception>(() => f.CreateClient());
    }

    [Fact]
    public async Task Will_Return_401_when_no_Auth_Passed()
    {
        var f = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Accounts:0:Name"] = Guid.NewGuid().ToString(),
                    ["Storage:Accounts:0:Key"] = Guid.NewGuid().ToString()
                });
            });
        });

        using var client = f.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Will_Return_403_when_no_Account_Key_Configured()
    {
        var client = GetBlobServiceClient(factory, "account", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==", new Dictionary<string, string?>
        {
            ["Storage:Accounts:0:Name"] = "account",
            ["Storage:Accounts:0:Key"] = ""
        });

        try
        {
            await client.GetAccountInfoAsync();
            Assert.Fail("Should have thrown.");
        }
        catch (RequestFailedException ex)
        {
            Assert.Equal(403, ex.Status);
        }
    }

    [Fact]
    public async Task Will_Return_403_when_Invalid_Account_Key_Passed()
    {
        var client = GetBlobServiceClient(factory, "account", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==", new Dictionary<string, string?>
        {
            ["Storage:Accounts:0:Name"] = "account",
            ["Storage:Accounts:0:Key"] = "Bby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
        });

        try
        {
            await client.GetAccountInfoAsync();
            Assert.Fail("Should have thrown.");
        }
        catch (RequestFailedException ex)
        {
            Assert.Equal(403, ex.Status);
        }
    }

    [Fact]
    public async Task Will_Return_403_when_Invalid_Account_Name_Passed()
    {
        var client = GetBlobServiceClient(factory, "some_other_account", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==", new Dictionary<string, string?>
        {
            ["Storage:Accounts:0:Name"] = "account",
            ["Storage:Accounts:0:Key"] = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="
        });

        try
        {
            await client.GetAccountInfoAsync();
            Assert.Fail("Should have thrown.");
        }
        catch (RequestFailedException ex)
        {
            Assert.Equal(403, ex.Status);
        }
    }

    [Fact]
    public async Task Will_Return_403_when_Malformed_Header_Passed()
    {
        var f = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Accounts:0:Name"] = Guid.NewGuid().ToString(),
                    ["Storage:Accounts:0:Key"] = Guid.NewGuid().ToString()
                });
            });
        });

        using var client = f.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "SharedKey asdf");
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static BlobServiceClient GetBlobServiceClient(WebApplicationFactory<Program> factory, string accountName, string accountKey, Dictionary<string, string?> configuration)
    {
        var f = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.AddInMemoryCollection(configuration);
            });
        });

        var httpClient = f.CreateClient();
        var options = new BlobClientOptions
        {
            Transport = new HttpClientTransport(httpClient)
        };

        var credential = new StorageSharedKeyCredential(accountName, accountKey);
        return new BlobServiceClient(httpClient.BaseAddress!, credential, options);
    }
}
