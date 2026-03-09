namespace Adliance.AzureBlobSimulator.Models;

public sealed class SasValidationResult
{
    public bool IsSuccess { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    private SasValidationResult(bool isSuccess, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static SasValidationResult Success()
        => new(true);

    public static SasValidationResult Fail(string code, string message)
        => new(false, code, message);
}
