namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Validates an XML document against an XSD schema.
/// </summary>
public interface IXsdValidator
{
    /// <summary>
    /// Validates <paramref name="xmlContent"/> against <paramref name="xsdContent"/>.
    /// </summary>
    /// <param name="xmlContent">The XML document to validate.</param>
    /// <param name="xsdContent">The XSD schema to validate against.</param>
    /// <returns>
    /// <c>true</c> if the XML is valid; otherwise <c>false</c>.
    /// </returns>
    bool Validate(string xmlContent, string xsdContent);

    /// <summary>
    /// Validates <paramref name="xmlContent"/> against <paramref name="xsdContent"/> and
    /// returns a list of validation error messages (empty if valid).
    /// </summary>
    /// <param name="xmlContent">The XML document to validate.</param>
    /// <param name="xsdContent">The XSD schema to validate against.</param>
    /// <returns>A (possibly empty) list of validation error messages.</returns>
    IReadOnlyList<string> GetValidationErrors(string xmlContent, string xsdContent);
}
