namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Validates an XML document against an XSD schema.
/// </summary>
public interface IXsdValidator
{
    /// <summary>
    /// Validates the given XML content against the provided XSD schema.
    /// </summary>
    /// <param name="xmlContent">The XML content to validate.</param>
    /// <param name="xsdContent">The XSD schema content to validate against.</param>
    /// <returns>
    /// A tuple where <c>IsValid</c> indicates whether the XML is valid,
    /// and <c>Errors</c> contains a list of validation error messages if any.
    /// </returns>
    (bool IsValid, IReadOnlyList<string> Errors) Validate(string xmlContent, string xsdContent);
}
