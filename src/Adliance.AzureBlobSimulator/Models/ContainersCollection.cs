using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class ContainersCollection
{
    [XmlElement("Container")]
    public List<Container> Container { get; set; } = new();
}
