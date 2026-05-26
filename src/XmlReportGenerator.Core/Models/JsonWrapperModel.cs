namespace XmlReportGenerator.Core.Models;

/// <summary>
/// The predefined JSON wrapper that encapsulates the Base64-encoded JSON payload.
/// </summary>
public class JsonWrapperModel
{
    /// <summary>Gets or sets the schema version of this wrapper.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the UTC timestamp when the payload was generated.</summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the Base64-encoded JSON representation of the XML data.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the encoding used for the payload (always "base64").</summary>
    public string Encoding { get; set; } = "base64";

    /// <summary>Gets or sets optional metadata about the source document.</summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
