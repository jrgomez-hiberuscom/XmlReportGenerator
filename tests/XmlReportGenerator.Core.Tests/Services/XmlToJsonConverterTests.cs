using FluentAssertions;
using Xunit;
using XmlReportGenerator.Core.Services;

namespace XmlReportGenerator.Core.Tests.Services;

public class XmlToJsonConverterTests
{
    private readonly XmlToJsonConverter _sut = new();

    [Fact]
    public void Convert_ValidXml_ReturnsJsonString()
    {
        // Arrange
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <root>
                <name>John Doe</name>
                <age>30</age>
            </root>
            """;

        // Act
        var result = _sut.Convert(xml);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("John Doe");
        result.Should().Contain("30");
    }

    [Fact]
    public void Convert_SimpleElement_ProducesValidJson()
    {
        // Arrange
        const string xml = "<greeting>Hello</greeting>";

        // Act
        var result = _sut.Convert(xml);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Hello");
    }

    [Fact]
    public void Convert_XmlWithAttributes_IncludesAttributesInJson()
    {
        // Arrange
        const string xml = """<person id="42"><name>Jane</name></person>""";

        // Act
        var result = _sut.Convert(xml);

        // Assert
        result.Should().Contain("42");
        result.Should().Contain("Jane");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Convert_NullOrEmptyXml_ThrowsArgumentException(string? xmlContent)
    {
        // Act
        var act = () => _sut.Convert(xmlContent!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
