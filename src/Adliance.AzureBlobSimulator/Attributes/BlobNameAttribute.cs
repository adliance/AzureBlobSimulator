using System.ComponentModel.DataAnnotations;

namespace Adliance.AzureBlobSimulator.Attributes;

public class BlobNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string valueAsString)
        {
            if (valueAsString.Length <= 0) return false;
            if (valueAsString.Length > 1024) return false;
            if (valueAsString.Contains('/')) return false;
            if (valueAsString.Contains('\\')) return false;
            if (valueAsString.Contains("..")) return false;

            return true;
        }

        return false;
    }
}
