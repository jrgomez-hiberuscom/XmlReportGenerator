namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Generates a dynamic Blazor component (.razor) that reproduces the Crystal Reports HTML
/// output from the provided XML data using AI (Semantic Kernel + LLM).
/// </summary>
public interface IBlazorComponentGenerator
{
    /// <summary>
    /// Generates a Blazor component that visually reproduces the given HTML output
    /// using the XML data as its source.
    /// </summary>
    /// <param name="xmlContent">The XML data used to drive the component's rendering.</param>
    /// <param name="referenceHtml">The reference HTML produced by Crystal Reports.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The content of the generated .razor component file.</returns>
    Task<string> GenerateComponentAsync(string xmlContent, string referenceHtml, CancellationToken cancellationToken = default);
}
