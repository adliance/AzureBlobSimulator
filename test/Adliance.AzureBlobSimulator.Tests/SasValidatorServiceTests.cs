using System.Text;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Tests;

public class SasValidatorServiceTests
{
    private readonly SasValidatorService _validator;
    private readonly IOptions<StorageOptions> _options;

    public SasValidatorServiceTests()
    {
        _options = Options.Create(new StorageOptions
        {
            Accounts =
            [
                new StorageAccountOptions
                {
                    Name = "testaccount",
                    Key = Convert.ToBase64String(Encoding.UTF8.GetBytes("1234567890123456"))
                }
            ]
        });

        var env = new TestHostEnvironment();
        _validator = new SasValidatorService(_options, null, env);
    }

    [Theory]
    [InlineData("GET", "rl")]
    [InlineData("PUT", "wc")]
    [InlineData("DELETE", "d")]
    [InlineData("HEAD", "r")]
    public void Validate_ValidSas_ReturnsSuccess(string method, string sp)
    {
        const string sv = "2026-02-06";
        const string se = "2099-01-01T00:00:00Z";
        const string sr = "b";
        var sig = GenerateSignature(sp, null, se, null, sv, sr);
        var request = new DefaultHttpContext().Request;
        request.Path = "/testaccount/mycontainer/myblob.txt";
        request.QueryString = new QueryString($"?sv={sv}&sp={sp}&se={se}&sr={sr}&sig=" + Uri.EscapeDataString(sig));
        request.Method = method;
        request.HttpContext.Items.Add("account", "testaccount"); // since middleware is not called, we set account manually

        var result = _validator.Validate(request);

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Fact]
    public void Validate_InvalidSas_ReturnsFail()
    {
        var request = new DefaultHttpContext().Request;
        request.Path = "/blob/testaccount/mycontainer/myblob.txt";
        request.QueryString = new QueryString("?sv=2026-02-06&sp=rw&se=2099-01-01T00:00:00Z&sr=b&sig=INVALID");

        var result = _validator.Validate(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("AuthenticationFailed", result.ErrorCode);
    }

    [Fact]
    public void CanBuildSignatureForContainer()
    {
        const string sp = "rw";
        var st = new DateTime(2026, 02, 13).ToString("o");
        var se = new DateTime(2099, 01, 01).ToString("o");
        const string spr = "http";
        const string sv = "2026-02-06";
        const string sr = "c";

        var sig = GenerateSignature(sp, st, se, spr, sv, sr);
        Assert.Equal("QK09Vp4+pLc0V985hU/SAJ2Sc1V9sXHkolOvWLljffE=", sig);
    }

    [Fact]
    public void CanBuildSignatureForBlob()
    {
        const string sp = "rw";
        var st = new DateTime(2026, 02, 13).ToString("o");
        var se = new DateTime(2099, 01, 01).ToString("o");
        const string spr = "http";
        const string sv = "2026-02-06";
        const string sr = "b";

        var sig = GenerateSignature(sp, st, se, spr, sv, sr);
        Assert.Equal("E6EBdQjZ5lZwxlM/MgexEHrhFhQRIMxPPuKQLNfjJws=", sig);
    }

    [Fact(Skip = "This test is only used to generate URL for manual testing")]
    public void GenerateUrlForManualTesting()
    {
        const string sp = "r";
        const string se = "2099-01-01T00:00:00.0000000Z";
        const string sv = "2026-02-06";
        const string sr = "b";

        var sig = GenerateSignature(sp, null, se, null, sv, sr, accountKey: "MTIzNDU2Nzg5MDEyMzQ1Ng==");
        var escapedSig = Uri.EscapeDataString(sig);

        var sasUrl = $"http://localhost:10000/testaccount/mycontainer/myblob.txt?sv={sv}&sp={sp}&se={se}&sr={sr}&sig={escapedSig}";
        _ = sasUrl;
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
