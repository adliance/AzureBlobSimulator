using System.Security.Cryptography;
using System.Text;
using Adliance.AzureBlobSimulator.Models;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Middleware;

public class AzureStorageAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AzureStorageAuthenticationMiddleware> _logger;
    private readonly List<StorageAccountOptions> _storageAccounts;

    public AzureStorageAuthenticationMiddleware(RequestDelegate next, IOptions<StorageOptions> blobStorageOptions, ILogger<AzureStorageAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;

        var options = blobStorageOptions.Value;
        _storageAccounts = options.Accounts;

        if (_storageAccounts.Count == 0)
        {
            throw new Exception("No storage accounts configured. Please configure at least one account in BlobStorage:Accounts array.");
        }

        _logger.LogInformation("Configured {AccountCount} storage accounts", _storageAccounts.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Check for SAS token authentication
        if (HasSasToken(request))
        {
            if (!ValidateSasToken(request))
            {
                await WriteAuthenticationError(context, "SAS token validation failed");
                return;
            }
        }
        // Check for SharedKey authentication
        else if (HasSharedKeyAuth(request))
        {
            if (!ValidateSharedKeyAuth(request))
            {
                await WriteAuthenticationError(context, "SharedKey authentication failed");
                return;
            }
        }
        else
        {
            await WriteAuthenticationError(context, "Missing authentication");
            return;
        }

        await _next(context);
    }

    private static bool HasSasToken(HttpRequest request)
    {
        return request.Query.ContainsKey("sig") && request.Query.ContainsKey("sv");
    }

    private static bool HasSharedKeyAuth(HttpRequest request)
    {
        return request.Headers.ContainsKey("Authorization") && request.Headers["Authorization"].ToString().StartsWith("SharedKey ");
    }

    private bool ValidateSasToken(HttpRequest request)
    {
        try
        {
            // Extract SAS parameters
            var sig = request.Query["sig"].ToString();
            var sv = request.Query["sv"].ToString(); // service version
            var se = request.Query["se"].ToString(); // expiry
            var sp = request.Query["sp"].ToString(); // permissions

            if (string.IsNullOrEmpty(sig) || string.IsNullOrEmpty(sv))
                return false;

            // Check expiry time
            if (!string.IsNullOrEmpty(se))
            {
                if (DateTime.TryParseExact(se, "yyyy-MM-ddTHH:mm:ssZ", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var expiryTime))
                {
                    if (DateTime.UtcNow > expiryTime)
                    {
                        _logger.LogWarning("SAS token expired: {ExpiryTime}", expiryTime);
                        return false;
                    }
                }
            }

            // Try all configured accounts for SAS validation
            foreach (var account in _storageAccounts)
            {
                var stringToSign = BuildSasStringToSign(request, sv, se, sp, account.Name);
                var expectedSignature = CalculateHmacSha256(stringToSign, account.Key);

                if (sig == expectedSignature)
                {
                    _logger.LogDebug("SAS token validated successfully for account: {AccountName}", account.Name);
                    return true;
                }
            }

            _logger.LogWarning("SAS signature validation failed for all configured accounts");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SAS token");
            return false;
        }
    }

    private bool ValidateSharedKeyAuth(HttpRequest request)
    {
        try
        {
            var authHeader = request.Headers["Authorization"].ToString();
            if (!authHeader.StartsWith("SharedKey ")) return false;

            var authValue = authHeader.Substring(10); // Remove "SharedKey "
            var parts = authValue.Split(':', 2);
            if (parts.Length != 2) return false;

            var accountName = parts[0];
            var providedSignature = parts[1];

            // Find matching account and validate
            var matchingAccount = _storageAccounts.FirstOrDefault(a => a.Name == accountName);
            if (matchingAccount == null)
            {
                _logger.LogWarning("Unknown account name: {AccountName}", accountName);
                return false;
            }

            // Build string-to-sign
            var stringToSign = BuildSharedKeyStringToSign(request, accountName);
            var expectedSignature = CalculateHmacSha256(stringToSign, matchingAccount.Key);

            var result = providedSignature == expectedSignature;
            if (!result)
            {
                _logger.LogWarning("SharedKey signature mismatch for account: {AccountName}", accountName);
                _logger.LogDebug("String to sign: {StringToSign}", stringToSign);
                _logger.LogDebug("Expected signature: {Expected}", expectedSignature);
                _logger.LogDebug("Provided signature: {Provided}", providedSignature);
            }
            else
            {
                _logger.LogDebug("SharedKey validated successfully for account: {AccountName}", accountName);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SharedKey authentication");
            return false;
        }
    }

    private static string BuildSharedKeyStringToSign(HttpRequest request, string accountName)
    {
        var verb = request.Method.ToUpper();
        var contentEncoding = GetHeaderValue(request, "Content-Encoding");
        var contentLanguage = GetHeaderValue(request, "Content-Language");
        var contentLength = request.ContentLength?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0";
        var contentMd5 = GetHeaderValue(request, "Content-MD5");
        var contentType = GetHeaderValue(request, "Content-Type");
        // var date = GetHeaderValue(request, "Date");
        var ifModifiedSince = GetHeaderValue(request, "If-Modified-Since");
        var ifMatch = GetHeaderValue(request, "If-Match");
        var ifNoneMatch = GetHeaderValue(request, "If-None-Match");
        var ifUnmodifiedSince = GetHeaderValue(request, "If-Unmodified-Since");
        var range = GetHeaderValue(request, "Range");

        // Use x-ms-date if present, otherwise use Date header
        //if (request.Headers.ContainsKey("x-ms-date"))
        //{
        var date = "";
        //}
        if (contentLength == "0") contentLength = "";

        var canonicalizedHeaders = BuildCanonicalizedHeaders(request);
        var canonicalizedResource = BuildCanonicalizedResource(request, accountName);

        var stringToSign = $"{verb}\n{contentEncoding}\n{contentLanguage}\n{contentLength}\n" +
                           $"{contentMd5}\n{contentType}\n{date}\n{ifModifiedSince}\n" +
                           $"{ifMatch}\n{ifNoneMatch}\n{ifUnmodifiedSince}\n{range}\n" +
                           $"{canonicalizedHeaders}{canonicalizedResource}";

        return stringToSign;
    }

    private static string BuildSasStringToSign(HttpRequest request, string sv, string se, string sp, string accountName)
    {
        // Simplified SAS string-to-sign construction
        // This is a basic implementation - full SAS validation would require more comprehensive logic
        var resource = $"/{accountName}{request.Path}";
        var permissions = sp;
        var expiry = se;
        var version = sv;

        return $"{permissions}\n\n{expiry}\n{resource}\n\n\n\n{version}";
    }

    private static string BuildCanonicalizedHeaders(HttpRequest request)
    {
        var canonicalizedHeaders = new SortedDictionary<string, string>();

        foreach (var header in request.Headers)
        {
            var headerName = header.Key.ToLowerInvariant();
            if (headerName.StartsWith("x-ms-"))
            {
                var headerValue = string.Join(",", header.Value.ToArray());
                canonicalizedHeaders[headerName] = headerValue.Trim();
            }
        }

        var result = new StringBuilder();
        foreach (var header in canonicalizedHeaders)
        {
            result.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{header.Key}:{header.Value}");
        }

        return result.ToString();
    }

    private static string BuildCanonicalizedResource(HttpRequest request, string accountName)
    {
        var resource = $"/{accountName}{request.Path}";

        if (request.QueryString.HasValue)
        {
            var queryParams = new SortedDictionary<string, string>();

            foreach (var param in request.Query)
            {
                var key = param.Key.ToLowerInvariant();
                var value = string.Join(",", param.Value.ToArray());
                queryParams[key] = value;
            }

            foreach (var param in queryParams)
            {
                resource += $"\n{param.Key}:{param.Value}";
            }
        }

        return resource;
    }

    private static string? GetHeaderValue(HttpRequest request, string headerName)
    {
        return request.Headers.TryGetValue(headerName, out var values) ? values.ToString() : null;
    }

    private static string CalculateHmacSha256(string stringToSign, string key)
    {
        var keyBytes = Convert.FromBase64String(key);
        var stringToSignBytes = Encoding.UTF8.GetBytes(stringToSign);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(stringToSignBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static async Task WriteAuthenticationError(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.Headers["x-ms-version"] = "2023-11-03";
        context.Response.Headers["x-ms-error-code"] = "AuthenticationFailed";
        context.Response.ContentType = "application/xml";

        var errorXml = $"""
                        <?xml version="1.0" encoding="utf-8"?>
                        <Error>
                            <Code>AuthenticationFailed</Code>
                            <Message>Server failed to authenticate the request. Make sure the value of Authorization header is formed correctly including the signature.</Message>
                            <AuthenticationErrorDetail>{message}</AuthenticationErrorDetail>
                        </Error>
                        """;

        await context.Response.WriteAsync(errorXml);
    }
}
