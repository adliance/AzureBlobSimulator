using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class Container
{
    public Container() {}

    public Container(string directoryPath)
    {
        Name = Path.GetFileName(directoryPath);
        Properties = new ContainerProperties(directoryPath);
    }

    [XmlElement("Name")] public string Name { get; set; } = string.Empty;
    [XmlElement("Properties")] public ContainerProperties Properties { get; set; } = new();
}
