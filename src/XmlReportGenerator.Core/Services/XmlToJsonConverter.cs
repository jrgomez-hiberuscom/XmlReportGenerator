using Newtonsoft.Json;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Core.Services;

/// <summary>
/// Converts XML documents to JSON using Newtonsoft.Json.
/// </summary>
public class XmlToJsonConverter : IXmlToJsonConverter
{
    /// <inheritdoc />
    public string Convert(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content must not be null or empty.", nameof(xmlContent));

        var xmlDocument = new System.Xml.XmlDocument();
        xmlDocument.LoadXml(xmlContent);

        return JsonConvert.SerializeXmlNode(xmlDocument, Formatting.Indented) ?? "{}";
    }
}
