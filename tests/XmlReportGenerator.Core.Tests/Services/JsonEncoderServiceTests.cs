using FluentAssertions;
using Xunit;
using XmlReportGenerator.Core.Services;

namespace XmlReportGenerator.Core.Tests.Services;

public class JsonEncoderServiceTests
{
    private readonly JsonEncoderService _sut = new();

    [Fact]
    public void Encode_ValidJson_ReturnsWrapperWithBase64Payload()
    {
        // Arrange
        const string json = """{"name":"John","age":30}""";

        // Act
        var result = _sut.Encode(json);

        // Assert
        result.Should().NotBeNull();
        result.Payload.Should().NotBeNullOrEmpty();
        result.Encoding.Should().Be("base64");
    }

    [Fact]
    public void Encode_ValidJson_PayloadDecodesBackToOriginal()
    {
        // Arrange
        const string json = """{"key":"value"}""";

        // Act
        var result = _sut.Encode(json);

        // Assert
        var decoded = System.Text.Encoding.UTF8.GetString(
            System.Convert.FromBase64String(result.Payload));
        decoded.Should().Be(json);
    }

    [Fact]
    public void Encode_ValidJson_SetsGeneratedAtToRecentUtcTime()
    {
        // Arrange
        const string json = "{}";
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = _sut.Encode(json);

        // Assert
        result.GeneratedAt.Should().BeAfter(before);
        result.GeneratedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Encode_ValidJson_DefaultVersionIsSet()
    {
        // Arrange
        const string json = "{}";

        // Act
        var result = _sut.Encode(json);

        // Assert
        result.Version.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Encode_NullOrEmptyJson_ThrowsArgumentException(string? jsonContent)
    {
        // Act
        var act = () => _sut.Encode(jsonContent!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
