namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Generates a Blazor component (.razor) that reproduces report HTML from XML data using AI.
/// </summary>
public interface IBlazorComponentGenerator
{
    /// <summary>
    /// Generates a Blazor component that reproduces the same visual output as
    /// <paramref name="referenceHtml"/> using data bound from <paramref name="xmlContent"/>.
    /// </summary>
    /// <param name="xmlContent">The XML document that contains the data to display.</param>
    /// <param name="referenceHtml">The reference HTML produced by Crystal Reports.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The content of the generated <c>.razor</c> file.</returns>
    Task<string> GenerateAsync(
        string xmlContent,
        string referenceHtml,
        CancellationToken cancellationToken = default);
}
