using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): the key a pipeline states is a template over the run's parameters, <c>{{name}}</c> for each value. Nothing completes a name the run did not
/// state: a parameter with no value is a coded failure, so two runs never meet on a key made of a guess.
/// </summary>
public sealed class PipelineRunClaimKeyTemplateTests
{
    private static Dictionary<string, object?> Values(params (string Name, object? Value)[] values) =>
        values.ToDictionary(pair => pair.Name, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATemplateTakesEachNamedParametersValueAsText()
    {
        var expanded = PipelineRunClaimKeyTemplate.Expand("chat:{{conversation}}:{{round}}", Values(("conversation", "c-9"), ("round", 3)), NullLogger.Instance);

        expanded.IsSuccess.ShouldBeTrue(expanded.CurrentMessage);
        expanded.Value.ShouldBe("chat:c-9:3");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterNameIsReadWithoutRegardToCase()
    {
        PipelineRunClaimKeyTemplate.Expand("{{Conversation}}", Values(("conversation", "c-9")), NullLogger.Instance).Value.ShouldBe("c-9");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATemplateWithNoPlaceholderIsTheKeyItself()
    {
        PipelineRunClaimKeyTemplate.Expand("nightly-load", Values(), NullLogger.Instance).Value.ShouldBe("nightly-load");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AParameterThatHasNoValueRefusesTheKeyNamingIt(string? value)
    {
        var expanded = PipelineRunClaimKeyTemplate.Expand("chat:{{conversation}}", Values(("conversation", value)), NullLogger.Instance);

        expanded.IsFailure.ShouldBeTrue();
        expanded.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60872");
        expanded.CurrentMessage.ShouldNotBeNull().ShouldContain("conversation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterTheRunDidNotStateRefusesTheKeyNamingIt()
    {
        var expanded = PipelineRunClaimKeyTemplate.Expand("chat:{{conversation}}", Values(("round", 1)), NullLogger.Instance);

        expanded.IsFailure.ShouldBeTrue();
        expanded.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60872");
        expanded.CurrentMessage.ShouldNotBeNull().ShouldContain("conversation");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("chat:{{conversation")]
    [InlineData("chat:{{}}")]
    [InlineData("chat:{{ }}")]
    public void ATemplateWithAnUnclosedOrNamelessPlaceholderIsRefusedWithItsCode(string template)
    {
        var expanded = PipelineRunClaimKeyTemplate.Expand(template, Values(("conversation", "c-9")), NullLogger.Instance);

        expanded.IsFailure.ShouldBeTrue();
        expanded.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60873");
    }
}
