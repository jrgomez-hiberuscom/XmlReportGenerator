using System.Text;
using Newtonsoft.Json;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Services;

/// <summary>
/// Encodes JSON to Base64 and wraps the result in a <see cref="JsonWrapperModel"/>.
/// </summary>
public class JsonEncoderService : IJsonEncoderService
{
    /// <inheritdoc />
    public string EncodeToBase64(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            throw new ArgumentException("JSON content must not be null or empty.", nameof(jsonContent));

        var bytes = Encoding.UTF8.GetBytes(jsonContent);
        return System.Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public string WrapInJson(string base64Payload, Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(base64Payload))
            throw new ArgumentException("Base64 payload must not be null or empty.", nameof(base64Payload));

        var wrapper = new JsonWrapperModel
        {
            Payload = base64Payload,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        return JsonConvert.SerializeObject(wrapper, Formatting.Indented);
    }
}
