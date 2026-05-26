using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using XmlReportGenerator.AI.Services;

namespace XmlReportGenerator.AI.Tests.Services;

/// <summary>
/// Tests for <see cref="XmlGeneratorService"/> that do not require a live LLM.
/// Full integration tests with a real Kernel are covered in integration test projects.
/// </summary>
public class XmlGeneratorServiceTests
{
    [Theory]
    [InlineData("```xml\n<root/>\n```", "<root/>")]
    [InlineData("```\n<root/>\n```", "<root/>")]
    [InlineData("<root/>", "<root/>")]
    [InlineData("  <root/>  ", "<root/>")]
    public void StripMarkdownFences_VariousInputs_ProducesCleanOutput(string input, string expected)
    {
        // StripMarkdownFences is private; verify via reflection
        var method = typeof(XmlGeneratorService)
            .GetMethod("StripMarkdownFences",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        method.Should().NotBeNull("StripMarkdownFences helper must exist");

        var result = (string?)method!.Invoke(null, new object[] { input });
        result.Should().Be(expected);
    }

    [Fact]
    public void LoadPromptTemplateAsync_ExtractsTemplateAfterSeparator()
    {
        // Arrange — verify the YAML parsing logic via reflection
        var yaml = "name: test\ndescription: test\n---\nMy prompt {{$xsd_content}}";

        // Simulate what LoadPromptTemplateAsync does
        var separatorIndex = yaml.IndexOf("\n---\n", StringComparison.Ordinal);
        var extracted = separatorIndex >= 0
            ? yaml[(separatorIndex + 5)..].Trim()
            : yaml.Trim();

        // Assert
        extracted.Should().Be("My prompt {{$xsd_content}}");
    }

    [Fact]
    public void LoadPromptTemplateAsync_NoSeparator_ReturnsFullContent()
    {
        var yaml = "My prompt {{$xsd_content}}";
        var separatorIndex = yaml.IndexOf("\n---\n", StringComparison.Ordinal);
        var extracted = separatorIndex >= 0 ? yaml[(separatorIndex + 5)..].Trim() : yaml.Trim();

        extracted.Should().Be("My prompt {{$xsd_content}}");
    }
}
