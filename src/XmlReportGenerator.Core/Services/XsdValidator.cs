using System.Xml;
using System.Xml.Schema;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Core.Services;

/// <summary>
/// Validates XML content against an XSD schema using <see cref="System.Xml.Schema.XmlSchema"/>.
/// </summary>
public class XsdValidator : IXsdValidator
{
    /// <inheritdoc />
    public (bool IsValid, IReadOnlyList<string> Errors) Validate(string xmlContent, string xsdContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));
        if (string.IsNullOrWhiteSpace(xsdContent))
            throw new ArgumentException("XSD content cannot be null or empty.", nameof(xsdContent));

        var errors = new List<string>();

        var settings = new XmlReaderSettings();
        using var xsdReader = new StringReader(xsdContent);
        var schema = XmlSchema.Read(xsdReader, (_, e) => errors.Add(e.Message));
        if (schema != null)
            settings.Schemas.Add(schema);

        settings.ValidationType = ValidationType.Schema;
        settings.ValidationEventHandler += (_, e) => errors.Add(e.Message);

        try
        {
            using var xmlReader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (xmlReader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add(ex.Message);
        }

        return (errors.Count == 0, errors.AsReadOnly());
    }
}
