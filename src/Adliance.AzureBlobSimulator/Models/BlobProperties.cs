using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class BlobProperties
{
    [XmlElement("Last-Modified")]
    public string LastModified { get; set; } = string.Empty;

    [XmlElement("Etag")]
    public string Etag { get; set; } = string.Empty;

    [XmlElement("Content-Length")]
    public long ContentLength { get; set; }

    [XmlElement("Content-Type")]
    public string ContentType { get; set; } = string.Empty;

    [XmlElement("BlobType")]
    public string BlobType { get; set; } = string.Empty;
}
