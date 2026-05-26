using FluentAssertions;
using Microsoft.SemanticKernel;
using Moq;
using Xunit;
using XmlReportGenerator.AI.Services;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Tests.Services;

public class XmlGeneratorServiceTests
{
    [Fact]
    public void XmlGeneratorService_ImplementsInterface()
    {
        // This test verifies that XmlGeneratorService implements the correct interface.
        // A real Kernel is required for integration tests; here we verify the contract.
        typeof(XmlGeneratorService).Should().Implement<IXmlGeneratorService>();
    }

    [Fact]
    public void XmlGeneratorService_HasCorrectConstructorDependency()
    {
        // Verify the constructor accepts a Kernel parameter
        var constructors = typeof(XmlGeneratorService).GetConstructors();
        constructors.Should().HaveCount(1);

        var parameters = constructors[0].GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(Kernel));
    }

    [Fact]
    public void BlazorComponentGenerator_ImplementsInterface()
    {
        typeof(XmlReportGenerator.AI.Services.BlazorComponentGenerator)
            .Should().Implement<IBlazorComponentGenerator>();
    }

    [Fact]
    public void BlazorComponentGenerator_HasCorrectConstructorDependency()
    {
        var constructors = typeof(XmlReportGenerator.AI.Services.BlazorComponentGenerator).GetConstructors();
        constructors.Should().HaveCount(1);

        var parameters = constructors[0].GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(Kernel));
    }
}
