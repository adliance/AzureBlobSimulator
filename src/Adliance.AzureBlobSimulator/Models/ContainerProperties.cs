using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Models;

public class ContainerProperties
{
    public ContainerProperties()
    {
    }

    public ContainerProperties(string directoryPath)
    {
        LastModified = DateTime.UtcNow.ToString("R");
        if (Directory.Exists(directoryPath)) LastModified = Directory.GetLastWriteTimeUtc(directoryPath).ToString("R");
        Etag = Guid.NewGuid().ToString();
        LeaseStatus = "unlocked";
        LeaseState = "available";
    }

    [XmlElement("Last-Modified")] public string LastModified { get; set; } = string.Empty;
    [XmlElement("Etag")] public string Etag { get; set; } = string.Empty;
    [XmlElement("LeaseStatus")] public string LeaseStatus { get; set; } = string.Empty;
    [XmlElement("LeaseState")] public string LeaseState { get; set; } = string.Empty;
}
