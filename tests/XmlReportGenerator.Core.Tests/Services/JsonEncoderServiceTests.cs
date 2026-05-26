using FluentAssertions;
using Xunit;
using XmlReportGenerator.Core.Services;

namespace XmlReportGenerator.Core.Tests.Services;

public class JsonEncoderServiceTests
{
    private readonly JsonEncoderService _sut = new();

    [Fact]
    public void EncodeToBase64_ValidJson_ReturnsBase64String()
    {
        // Arrange
        const string json = """{"key":"value"}""";

        // Act
        var base64 = _sut.EncodeToBase64(json);

        // Assert
        base64.Should().NotBeNullOrWhiteSpace();
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        decoded.Should().Be(json);
    }

    [Fact]
    public void EncodeToBase64_EmptyString_ThrowsArgumentException()
    {
        var act = () => _sut.EncodeToBase64(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WrapInJson_ValidBase64_ReturnsJsonWrapper()
    {
        // Arrange
        const string payload = "dGVzdA=="; // base64 of "test"

        // Act
        var wrapper = _sut.WrapInJson(payload);

        // Assert
        wrapper.Should().NotBeNullOrWhiteSpace();
        wrapper.Should().Contain("Payload");
        wrapper.Should().Contain(payload);
        wrapper.Should().Contain("CorrelationId");
        wrapper.Should().Contain("SchemaVersion");
    }

    [Fact]
    public void WrapInJson_WithMetadata_IncludesMetadataInOutput()
    {
        // Arrange
        const string payload = "dGVzdA==";
        var metadata = new Dictionary<string, string> { ["source"] = "test.xsd" };

        // Act
        var wrapper = _sut.WrapInJson(payload, metadata);

        // Assert
        wrapper.Should().Contain("test.xsd");
    }

    [Fact]
    public void WrapInJson_EmptyPayload_ThrowsArgumentException()
    {
        var act = () => _sut.WrapInJson(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundTrip_XmlToBase64ToWrapper_IsReversible()
    {
        // Arrange
        const string json = """{"data":"hello world"}""";

        // Act
        var base64 = _sut.EncodeToBase64(json);
        var wrapper = _sut.WrapInJson(base64);

        // Assert – the original JSON must be recoverable from the wrapper
        wrapper.Should().Contain(base64);
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        decoded.Should().Be(json);
    }
}
