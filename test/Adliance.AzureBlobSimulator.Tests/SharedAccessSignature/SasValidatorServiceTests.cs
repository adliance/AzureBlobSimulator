using System.Text;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Tests.SharedAccessSignature;

public class SasValidatorServiceTests
{
    private readonly SasHelper _sasHelper;
    private readonly SasValidatorService _validator;

    public SasValidatorServiceTests()
    {
        var options = Options.Create(new StorageOptions
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
        _validator = new SasValidatorService(options, null, env);
        _sasHelper = new SasHelper(options, _validator);
    }

    /// <summary>
    /// Validates that the SAS validator returns success for valid SAS tokens
    /// with various HTTP methods and permissions.
    /// </summary>
    [Theory]
    [InlineData("GET", "rl")]
    [InlineData("PUT", "wc")]
    [InlineData("DELETE", "d")]
    [InlineData("HEAD", "r")]
    public void Validate_WithValidSas_ReturnsSuccess(string method, string sp)
    {
        const string sv = "2026-02-06";
        const string se = "2099-01-01T00:00:00Z";
        const string sr = "b";
        var sig = _sasHelper.GenerateSignature(sp, null, se, null, sv, sr);
        var request = new DefaultHttpContext().Request;
        request.Path = "/testaccount/mycontainer/myblob.txt";
        request.QueryString = new QueryString($"?sv={sv}&sp={sp}&se={se}&sr={sr}&sig=" + Uri.EscapeDataString(sig));
        request.Method = method;
        request.HttpContext.Items.Add("account", "testaccount"); // since middleware is not called, set the account manually

        var result = _validator.Validate(request);

        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    /// <summary>
    /// Validates that the SAS validator fails when an invalid SAS token is provided.
    /// </summary>
    [Fact]
    public void Validate_WithInvalidSas_ReturnsFailure()
    {
        var request = new DefaultHttpContext().Request;
        request.Path = "/blob/testaccount/mycontainer/myblob.txt";
        request.QueryString = new QueryString("?sv=2026-02-06&sp=rw&se=2099-01-01T00:00:00Z&sr=b&sig=INVALID");

        var result = _validator.Validate(request);

        Assert.False(result.IsSuccess);
        Assert.Equal("AuthenticationFailed", result.ErrorCode);
    }

    /// <summary>
    /// Ensures a SAS signature can be correctly generated for a container resource.
    /// </summary>
    [Fact]
    public void GenerateSignature_ForContainer_ReturnsExpectedSignature()
    {
        const string sp = "rw";
        var st = new DateTime(2026, 02, 13).ToString("o");
        var se = new DateTime(2099, 01, 01).ToString("o");
        const string spr = "http";
        const string sv = "2026-02-06";
        const string sr = "c";

        var sig = _sasHelper.GenerateSignature(sp, st, se, spr, sv, sr);
        Assert.Equal("QK09Vp4+pLc0V985hU/SAJ2Sc1V9sXHkolOvWLljffE=", sig);
    }

    /// <summary>
    /// Ensures a SAS signature can be correctly generated for a blob resource.
    /// </summary>
    [Fact]
    public void GenerateSignature_ForBlob_ReturnsExpectedSignature()
    {
        const string sp = "rw";
        var st = new DateTime(2026, 02, 13).ToString("o");
        var se = new DateTime(2099, 01, 01).ToString("o");
        const string spr = "http";
        const string sv = "2026-02-06";
        const string sr = "b";

        var sig = _sasHelper.GenerateSignature(sp, st, se, spr, sv, sr);
        Assert.Equal("E6EBdQjZ5lZwxlM/MgexEHrhFhQRIMxPPuKQLNfjJws=", sig);
    }

    /// <summary>
    /// Generates a full SAS URL for manual testing and verifies the expected URL format.
    /// </summary>
    [Fact]
    public void GenerateSasUrl_ForBlob_ReturnsExpectedUrl()
    {
        const string sp = "r";
        const string se = "2099-01-01T00:00:00.0000000Z";
        const string sv = "2026-02-06";
        const string sr = "b";
        const string accountKey = "MTIzNDU2Nzg5MDEyMzQ1Ng==";
        const string baseUrl = "http://localhost:10000/testaccount/mycontainer/myblob.txt";

        var sig = _sasHelper.GenerateSignature(sp, null, se, null, sv, sr, accountKey: accountKey);
        var escapedSig = Uri.EscapeDataString(sig);
        var sasUrl = $"{baseUrl}?sv={sv}&sp={sp}&se={se}&sr={sr}&sig={escapedSig}";

        const string expectedSig = "En9zzpp9theXis59%2Fw%2FZ%2Fae%2FBJ15dn%2BEft3n2IOl2Ic%3D";
        const string expectedUrl = $"{baseUrl}?sv={sv}&sp={sp}&se={se}&sr={sr}&sig={expectedSig}";
        Assert.Equal(expectedUrl, sasUrl);
    }
}
