using FluentAssertions;
using Xunit;
using XmlReportGenerator.Core.Services;

namespace XmlReportGenerator.Core.Tests.Services;

public class XmlToJsonConverterTests
{
    private readonly XmlToJsonConverter _sut = new();

    [Fact]
    public void Convert_SimpleXml_ReturnsJson()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
              <item id="1">Hello</item>
            </root>
            """;

        // Act
        var json = _sut.Convert(xml);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("root");
        json.Should().Contain("Hello");
    }

    [Fact]
    public void Convert_NullOrEmptyXml_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => _sut.Convert(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Convert_InvalidXml_ThrowsException()
    {
        // Arrange
        const string invalidXml = "<unclosed";

        // Act & Assert
        var act = () => _sut.Convert(invalidXml);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Convert_XmlWithAttributes_PreservesAttributeData()
    {
        // Arrange
        const string xml = "<person name=\"Alice\" age=\"30\" />";

        // Act
        var json = _sut.Convert(xml);

        // Assert
        json.Should().Contain("Alice");
        json.Should().Contain("30");
    }
}
