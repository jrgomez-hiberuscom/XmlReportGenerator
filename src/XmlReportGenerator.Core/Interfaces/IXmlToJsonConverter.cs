namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Converts an XML document to its JSON representation.
/// </summary>
public interface IXmlToJsonConverter
{
    /// <summary>
    /// Converts an XML string to a JSON string.
    /// </summary>
    /// <param name="xmlContent">The XML content to convert.</param>
    /// <returns>A JSON string representing the XML document.</returns>
    string Convert(string xmlContent);
}
