using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class Container
{
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Properties")]
    public ContainerProperties Properties { get; set; } = new();
}
