using System.ComponentModel.DataAnnotations;

namespace Adliance.AzureBlobSimulator.Attributes;

public class BlobNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string s)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Length > 1024)
        {
            return false;
        }

        if (s.StartsWith('/') || s.StartsWith('\\'))
        {
            return false;
        }

        if (s.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (s.Contains('\\'))
        {
            return false;
        }

        return true;
    }
}
