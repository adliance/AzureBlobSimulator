using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

[XmlRoot("EnumerationResults")]
public class BlobEnumerationResults
{
    [XmlAttribute("ServiceEndpoint")]
    public string ServiceEndpoint { get; set; } = string.Empty;

    [XmlAttribute("ContainerName")]
    public string ContainerName { get; set; } = string.Empty;

    [XmlElement("Blobs")]
    public BlobsCollection Blobs { get; set; } = new();
}
