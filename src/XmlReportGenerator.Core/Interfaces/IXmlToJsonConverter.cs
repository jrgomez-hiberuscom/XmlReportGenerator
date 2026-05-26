namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Converts an XML document to its JSON representation.
/// </summary>
public interface IXmlToJsonConverter
{
    /// <summary>
    /// Converts the provided XML string to a JSON string.
    /// </summary>
    /// <param name="xmlContent">A well-formed XML document.</param>
    /// <returns>The JSON representation of the XML document.</returns>
    string Convert(string xmlContent);
}
