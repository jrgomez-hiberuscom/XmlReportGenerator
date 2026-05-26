using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Generates a fictitious but schema-valid XML document using AI (Semantic Kernel + LLM).
/// </summary>
public interface IXmlGeneratorService
{
    /// <summary>
    /// Generates an XML document that is valid against <paramref name="xsdContent"/> using the
    /// natural-language instructions in <paramref name="markdownInstructions"/> as guidance.
    /// </summary>
    /// <param name="xsdContent">The full text of the XSD schema.</param>
    /// <param name="markdownInstructions">Natural-language instructions that describe the desired data.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A well-formed, schema-valid XML string.</returns>
    Task<string> GenerateXmlAsync(
        string xsdContent,
        string markdownInstructions,
        CancellationToken cancellationToken = default);
}
