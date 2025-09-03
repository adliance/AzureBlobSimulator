using System.ComponentModel.DataAnnotations;

namespace Adliance.AzureBlobSimulator.Attributes;

public class ContainerNameAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string valueAsString)
        {
            if (valueAsString.Length <= 0) return false;
            if (valueAsString.Length > 63) return false;
            if (valueAsString.Contains('/')) return false;
            if (valueAsString.Contains('\\')) return false;
            if (valueAsString.Contains("--")) return false;
            if (valueAsString.StartsWith('_') || valueAsString.EndsWith('_')) return false;
            if (valueAsString.StartsWith('-') || valueAsString.EndsWith('-')) return false;

            return true;
        }

        return false;
    }
}
