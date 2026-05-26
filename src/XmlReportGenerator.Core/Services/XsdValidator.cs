using System.Xml;
using System.Xml.Schema;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Core.Services;

/// <summary>
/// Validates XML documents against XSD schemas using <see cref="XmlSchemaSet"/>.
/// </summary>
public class XsdValidator : IXsdValidator
{
    /// <inheritdoc />
    public bool Validate(string xmlContent, string xsdContent)
        => GetValidationErrors(xmlContent, xsdContent).Count == 0;

    /// <inheritdoc />
    public IReadOnlyList<string> GetValidationErrors(string xmlContent, string xsdContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content must not be null or empty.", nameof(xmlContent));
        if (string.IsNullOrWhiteSpace(xsdContent))
            throw new ArgumentException("XSD content must not be null or empty.", nameof(xsdContent));

        var errors = new List<string>();

        var schemaSet = new XmlSchemaSet();
        using var xsdReader = new StringReader(xsdContent);
        schemaSet.Add(null, XmlReader.Create(xsdReader));

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };

        settings.ValidationEventHandler += (_, args) =>
        {
            if (args.Severity == XmlSeverityType.Error || args.Severity == XmlSeverityType.Warning)
                errors.Add(args.Message);
        };

        using var xmlReader = XmlReader.Create(new StringReader(xmlContent), settings);
        try
        {
            while (xmlReader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add(ex.Message);
        }

        return errors.AsReadOnly();
    }
}
