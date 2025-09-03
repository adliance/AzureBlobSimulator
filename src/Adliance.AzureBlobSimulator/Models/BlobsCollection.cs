using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class BlobsCollection
{
    public BlobsCollection() {}

    public BlobsCollection(List<Blob> blobs)
    {
        Blob = blobs;
    }

    [XmlElement("Blob")] public List<Blob> Blob { get; set; } = new();
}
