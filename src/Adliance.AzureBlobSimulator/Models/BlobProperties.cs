using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class BlobProperties
{
    [XmlElement("Last-Modified")] public string LastModified { get; init; } = string.Empty;
    [XmlElement("x-ms-creation-time")] public string Created { get; init; } = string.Empty;
    [XmlElement("Etag")] public string Etag { get; init; } = string.Empty;
    [XmlElement("Content-Length")] public long ContentLength { get; init; }
    [XmlElement("Content-Type")] public string ContentType { get; init; } = string.Empty;
    [XmlElement("BlobType")] public string BlobType { get; init; } = string.Empty;
}
