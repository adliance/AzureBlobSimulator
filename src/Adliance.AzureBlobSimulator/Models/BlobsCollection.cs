using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class BlobsCollection
{
    [XmlElement("Blob")]
    public List<Blob> Blob { get; set; } = new();
}
