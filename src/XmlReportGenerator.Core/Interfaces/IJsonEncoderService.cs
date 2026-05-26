using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Encodes a JSON payload into a Base64 string and wraps it in the predefined <see cref="JsonWrapperModel"/>.
/// </summary>
public interface IJsonEncoderService
{
    /// <summary>
    /// Encodes the given JSON string to Base64 and produces the standard JSON wrapper payload.
    /// </summary>
    /// <param name="jsonContent">The JSON string to encode.</param>
    /// <returns>A <see cref="JsonWrapperModel"/> containing the Base64-encoded payload.</returns>
    JsonWrapperModel Encode(string jsonContent);
}
