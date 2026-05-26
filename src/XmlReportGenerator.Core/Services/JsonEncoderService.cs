using System.Text;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Services;

/// <summary>
/// Encodes a JSON string to Base64 and wraps it in the predefined <see cref="JsonWrapperModel"/>.
/// </summary>
public class JsonEncoderService : IJsonEncoderService
{
    /// <inheritdoc />
    public JsonWrapperModel Encode(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            throw new ArgumentException("JSON content cannot be null or empty.", nameof(jsonContent));

        var bytes = Encoding.UTF8.GetBytes(jsonContent);
        var base64 = System.Convert.ToBase64String(bytes);

        return new JsonWrapperModel
        {
            Payload = base64,
            GeneratedAt = DateTime.UtcNow,
        };
    }
}
