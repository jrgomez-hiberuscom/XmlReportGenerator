using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Encodes a JSON string to Base64 and wraps it in the predefined <see cref="JsonWrapperModel"/>.
/// </summary>
public interface IJsonEncoderService
{
    /// <summary>
    /// Encodes <paramref name="jsonContent"/> as a Base64 string.
    /// </summary>
    /// <param name="jsonContent">A JSON string to encode.</param>
    /// <returns>The Base64-encoded representation of the JSON string (UTF-8 bytes).</returns>
    string EncodeToBase64(string jsonContent);

    /// <summary>
    /// Wraps the given Base64 payload inside a <see cref="JsonWrapperModel"/> and
    /// serialises the result to a JSON string.
    /// </summary>
    /// <param name="base64Payload">The Base64-encoded payload to embed.</param>
    /// <param name="metadata">Optional metadata entries to include in the wrapper.</param>
    /// <returns>A JSON string representing the complete <see cref="JsonWrapperModel"/>.</returns>
    string WrapInJson(string base64Payload, Dictionary<string, string>? metadata = null);
}
