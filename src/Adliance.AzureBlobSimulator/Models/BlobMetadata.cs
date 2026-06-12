namespace Adliance.AzureBlobSimulator.Models;

public class BlobMetadata
{
    public string? Path { get; set; }
    public string? Name { get; set; }

    public long ContentLength { get; set; }
    public string? ContentType { get; set; }

    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }

    public string? ETag { get; set; }
}
