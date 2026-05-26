namespace XmlReportGenerator.Core.Models;

/// <summary>
/// Represents the predefined JSON wrapper structure that embeds the Base64-encoded XML payload.
/// </summary>
public class JsonWrapperModel
{
    /// <summary>Gets or sets the unique correlation identifier for the pipeline run.</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the UTC timestamp when the wrapper was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the version of the wrapper schema.</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>Gets or sets the content type of the payload.</summary>
    public string ContentType { get; set; } = "application/xml+base64";

    /// <summary>Gets or sets the Base64-encoded JSON payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets optional metadata about the source files.</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
