namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Generates a fictitious but schema-valid XML document using AI (Semantic Kernel + LLM).
/// Reads the XSD schema and the Markdown instructions to produce a plausible XML payload.
/// </summary>
public interface IXmlGeneratorService
{
    /// <summary>
    /// Generates a valid XML string based on the provided XSD schema and Markdown instructions.
    /// </summary>
    /// <param name="xsdContent">The content of the XSD schema file.</param>
    /// <param name="markdownInstructions">The Markdown file content with generation instructions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid XML string that conforms to the given schema.</returns>
    Task<string> GenerateXmlAsync(string xsdContent, string markdownInstructions, CancellationToken cancellationToken = default);
}
