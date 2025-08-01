using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Adliance.AzureBlobSimulator.Services;

public static class XmlSerializationService
{
    public static string SerializeToXml<T>(T obj) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false), // No BOM
            Indent = true,
            OmitXmlDeclaration = false
        };

        using var memoryStream = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(memoryStream, settings);

        serializer.Serialize(xmlWriter, obj);
        xmlWriter.Flush();

        return new UTF8Encoding(false).GetString(memoryStream.ToArray());
    }
}
