using System.Security.Cryptography;
using System.Text;
using Adliance.AzureBlobSimulator.Models;
using Adliance.AzureBlobSimulator.Services;
using Microsoft.Extensions.Options;

namespace Adliance.AzureBlobSimulator.Middleware;

public class AzureStorageAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SharedKeyAuthService _sharedKeyAuthService;

    public AzureStorageAuthenticationMiddleware(RequestDelegate next, SharedKeyAuthService sharedKeyAuthService,  IOptions<StorageOptions> blobStorageOptions, ILogger<AzureStorageAuthenticationMiddleware> logger)
    {
        _next = next;
        _sharedKeyAuthService = sharedKeyAuthService;

        var options = blobStorageOptions.Value;
        var storageAccounts = options.Accounts;

        if (storageAccounts.Count == 0)
        {
            throw new Exception("No storage accounts configured. Please configure at least one account in BlobStorage:Accounts array.");
        }

        logger.LogDebug("Configured {AccountCount} storage accounts.", storageAccounts.Count);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // Allow unauthenticated health checks
        if (request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (_sharedKeyAuthService.HasSharedKeyAuth(request))
        {
            if (!_sharedKeyAuthService.ValidateSharedKeyAuth(request))
            {
                await WriteAuthenticationError(context, "SharedKey authentication failed.");
                return;
            }
        }
        else
        {
            await WriteAuthenticationError(context, "Authentication is missing.", 401);
            return;
        }

        await _next(context);
    }

    private static async Task WriteAuthenticationError(HttpContext context, string message, int statusCode = 403)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers["x-ms-version"] = Constants.MsVersion;
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
