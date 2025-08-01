using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

[XmlRoot("EnumerationResults")]
public class ContainerEnumerationResults
{
    [XmlAttribute("ServiceEndpoint")]
    public string ServiceEndpoint { get; set; } = string.Empty;

    [XmlElement("Containers")]
    public ContainersCollection Containers { get; set; } = new();
}
