using FluentAssertions;
using Xunit;
using XmlReportGenerator.AI.Services;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Tests.Services;

public class XmlGeneratorServiceTests
{
    [Fact]
    public void XmlGeneratorService_ImplementsInterface()
    {
        typeof(XmlGeneratorService).Should().Implement<IXmlGeneratorService>();
    }

    [Fact]
    public void XmlGeneratorService_HasCorrectConstructorDependency()
    {
        var constructors = typeof(XmlGeneratorService).GetConstructors();
        constructors.Should().HaveCount(1);

        var parameters = constructors[0].GetParameters();
        parameters.Should().HaveCount(2); // LiteLlmClient + ILogger
        parameters[0].ParameterType.Should().Be(typeof(LiteLlmClient));
    }

    [Fact]
    public void BlazorComponentGenerator_ImplementsInterface()
    {
        typeof(BlazorComponentGenerator).Should().Implement<IBlazorComponentGenerator>();
    }

    [Fact]
    public void BlazorComponentGenerator_HasCorrectConstructorDependency()
    {
        var constructors = typeof(BlazorComponentGenerator).GetConstructors();
        constructors.Should().HaveCount(1);

        var parameters = constructors[0].GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(LiteLlmClient));
    }
}
