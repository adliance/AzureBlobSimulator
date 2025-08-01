using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class Blob
{
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Properties")]
    public BlobProperties Properties { get; set; } = new();
}
