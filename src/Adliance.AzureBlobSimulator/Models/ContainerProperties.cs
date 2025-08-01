using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class ContainerProperties
{
    [XmlElement("Last-Modified")]
    public string LastModified { get; set; } = string.Empty;

    [XmlElement("Etag")]
    public string Etag { get; set; } = string.Empty;

    [XmlElement("LeaseStatus")]
    public string LeaseStatus { get; set; } = string.Empty;

    [XmlElement("LeaseState")]
    public string LeaseState { get; set; } = string.Empty;
}
