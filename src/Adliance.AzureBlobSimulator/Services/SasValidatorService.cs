using System.Security.Cryptography;
using System.Text;
using Adliance.AzureBlobSimulator.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Adliance.AzureBlobSimulator.Services;

public sealed class SasValidatorService(
    IOptions<StorageOptions> options,
    ILogger<SasValidatorService>? logger,
    IHostEnvironment environment)
{
    private readonly StorageOptions _options = options.Value;
    private readonly bool _debugEnabled = environment.IsDevelopment();

    public SasValidationResult Validate(HttpRequest request)
    {
        var account = ResolveAccount(request);
        if (account == null)
            return SasValidationResult.Fail("AuthenticationFailed", "Unknown storage account.");

        var accountKeyBytes = Convert.FromBase64String(account.Key);

        var query = request.Query;

        if (!query.TryGetValue("sig", out var sigValues) ||
            StringValues.IsNullOrEmpty(sigValues))
        {
            return SasValidationResult.Fail("AuthenticationFailed", "Missing signature.");
        }

        var signature = sigValues.ToString();

        if (!TryGetRequired(query, "sv", out var sv) ||
            !TryGetRequired(query, "sp", out var sp) ||
            !TryGetRequired(query, "se", out var se) ||
            !TryGetRequired(query, "sr", out var sr))
        {
            return SasValidationResult.Fail("AuthenticationFailed", "Missing required SAS parameters.");
        }

        TryGetOptional(query, "st", out var st);
        TryGetOptional(query, "spr", out var spr);

        var now = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(st) &&
            DateTimeOffset.TryParse(st, out var startTime))
        {
            if (now < startTime)
                return SasValidationResult.Fail("AuthenticationFailed", "SAS not yet valid.");
        }

        if (!DateTimeOffset.TryParse(se, out var expiryTime))
        {
            return SasValidationResult.Fail("AuthenticationFailed", "Invalid expiry time.");
        }

        if (now > expiryTime)
        {
            return SasValidationResult.Fail("AuthenticationFailed", "SAS expired.");
        }

        var canonicalizedResource = BuildCanonicalizedResource(request, account.Name, sr);
        if (canonicalizedResource == null)
        {
            return SasValidationResult.Fail("AuthenticationFailed", "Invalid resource.");
        }

        var stringToSign = BuildStringToSign(sp, st, se, canonicalizedResource, spr, sv, sr);

        var computedSignature = ComputeSignature(stringToSign, accountKeyBytes);

        if (_debugEnabled)
        {
            logger?.LogInformation(
                """
                SAS DEBUG
                Account: {Account}
                CanonicalizedResource: {CanonicalizedResource}
                StringToSign:
                {StringToSign}
                ComputedSignature: {ComputedSignature}
                ProvidedSignature: {ProvidedSignature}
                """,
                account.Name,
                canonicalizedResource,
                EscapeNewlines(stringToSign),
                computedSignature,
                signature
            );
        }

        if (!SignaturesEqual(computedSignature, signature))
        {
            return SasValidationResult.Fail("AuthenticationFailed", "Signature mismatch.");
        }

        if (!IsPermissionAllowed(request.Method, sp))
        {
            return SasValidationResult.Fail("AuthorizationPermissionMismatch", "Permission denied.");
        }

        return SasValidationResult.Success();
    }

    public string GenerateSignature(string sp,
        string? st,
        string se,
        string canonicalizedResource,
        string? spr,
        string sv,
        string sr,
        byte[] key)
    {
        var sts = BuildStringToSign(sp, st, se, canonicalizedResource, spr, sv, sr);
        return ComputeSignature(sts, key);
    }

    private StorageAccountOptions? ResolveAccount(HttpRequest request)
    {
        var possibleAccountName = request.HttpContext.Items["account"] as string;

        if (string.IsNullOrEmpty(possibleAccountName))
        {
            return null;
        }

        var account = _options.Accounts.FirstOrDefault(a =>
            string.Equals(a.Name, possibleAccountName, StringComparison.OrdinalIgnoreCase));

        return account;
    }

    private static string? BuildCanonicalizedResource(HttpRequest request, string accountName, string sr)
    {
        var path = request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        // Remove account-segment
        if (segments[0].Equals(accountName, StringComparison.OrdinalIgnoreCase))
        {
            segments = segments.Skip(1).ToArray();
        }

        if (segments.Length == 0)
        {
            return null;
        }

        var container = segments[0];

        switch (sr)
        {
            case "c":
                return $"/blob/{accountName}/{container}";
            case "b" when segments.Length < 2:
                return null;
            case "b":
            {
                var blobPath = string.Join("/", segments.Skip(1));
                return $"/blob/{accountName}/{container}/{blobPath}";
            }
            default:
                return null;
        }
    }

    private static string BuildStringToSign(
        string sp,
        string? st,
        string se,
        string canonicalizedResource,
        string? spr,
        string sv,
        string sr)
    {
        var sb = new StringBuilder();

        sb.AppendLine(sp); // permissions
        sb.AppendLine(st ?? string.Empty); // start time
        sb.AppendLine(se); // expiry time
        sb.AppendLine(canonicalizedResource); // canonicalized resource
        sb.AppendLine(string.Empty); // signed identifier (si)
        sb.AppendLine(string.Empty); // IP (sip)
        sb.AppendLine(spr ?? string.Empty); // signed protocol
        sb.AppendLine(sv); // version
        sb.AppendLine(sr); // resource (b/c)
        sb.AppendLine(string.Empty); // rscc
        sb.AppendLine(string.Empty); // rscd
        sb.AppendLine(string.Empty); // rsce
        sb.AppendLine(string.Empty); // rscl
        sb.AppendLine(string.Empty); // rsct

        return sb.ToString();
    }

    private static string ComputeSignature(string stringToSign, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
        return Convert.ToBase64String(hash);
    }

    private static bool SignaturesEqual(string computed, string provided)
    {
        try
        {
            var computedBytes = Convert.FromBase64String(computed);
            var providedBytes = Convert.FromBase64String(provided);

            return CryptographicOperations.FixedTimeEquals(computedBytes, providedBytes);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPermissionAllowed(string? method, string sp)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return false;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return sp.Contains('r') || sp.Contains('l');
        }

        if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            return sp.Contains('w') || sp.Contains('c');
        }

        if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            return sp.Contains('d');
        }

        return method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) && sp.Contains('r');
    }

    private static bool TryGetRequired(IQueryCollection query, string key, out string value)
    {
        if (query.TryGetValue(key, out var values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            value = values.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetOptional(IQueryCollection query, string key, out string? value)
    {
        if (query.TryGetValue(key, out var values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            value = values.ToString();
            return true;
        }

        value = null;
        return false;
    }

    private static string EscapeNewlines(string input)
        => input.Replace("\r", "\\r").Replace("\n", "\\n\n");
}
