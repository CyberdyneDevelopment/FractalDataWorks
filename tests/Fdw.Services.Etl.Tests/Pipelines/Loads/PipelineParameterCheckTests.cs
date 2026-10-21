using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Pipelines.Loads;

/// <summary>
/// Tests for <see cref="PipelineParameterCheck"/> (FDW-1745): a schedule's parameter values are judged against the declarations the
/// pipeline detail answers, and a declaration that states a list is judged as a list.
/// </summary>
public sealed class PipelineParameterCheckTests
{
    private static List<PipelineParameterDeclaration> Declared(bool isList) =>
        [new PipelineParameterDeclaration { Name = "Cols", Type = "String", IsList = isList }];

    private static List<PipelineParameterValue> Supplied(string value) => [new PipelineParameterValue { Name = "Cols", Value = value }];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AJsonArrayForAListParameterIsAccepted()
    {
        var result = PipelineParameterCheck.Check(Declared(isList: true), Supplied("[\"Name\",\"City\"]"), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void PlainTextForAListParameterIsRefused()
    {
        var result = PipelineParameterCheck.Check(Declared(isList: true), Supplied("Name"), NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AParameterNotDeclaredAsAListStillTakesSingleText()
    {
        var result = PipelineParameterCheck.Check(Declared(isList: false), Supplied("Name"), NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
    }
}
