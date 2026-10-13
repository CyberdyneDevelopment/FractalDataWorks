using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-1221: one <c>/transformations</c> resource serves every kind of transformation. The request's implementation picks the kind,
/// and a kind refuses the members that belong to another.
/// </summary>
public sealed class TransformationKindsTests
{
    private static TransformationKindContext Context()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, null, null)).Returns(500);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, null, null)).Returns(20000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, null, null)).Returns(32);
        return new TransformationKindContext(settings.Object, NullLogger.Instance);
    }

    private static List<TransformationInputPayload> Inputs() => [new() { Name = "price", ValueType = "Number" }];

    private static List<BuiltResultPayload> Results() =>
        [new() { Name = "total", IsKept = true, Formula = new BuiltFormulaNodePayload { Op = "Field", Name = "price" } }];

    [Fact]
    public void TheFieldAndBuiltKindsAreFoundByTheImplementationAName()
    {
        TransformationKinds.ByName("Field").ShouldBeOfType<FieldTransformationKind>();
        TransformationKinds.ByName("Built").ShouldBeOfType<BuiltTransformationKind>();
        ReferenceEquals(TransformationKinds.ByName("Map"), TransformationKinds.NotFound).ShouldBeTrue();
    }

    [Fact]
    public void ARequestThatNamesNoKindOrAnUnknownOneIsRefused()
    {
        TransformationEndpointSupport.FindKind(null, NullLogger.Instance).IsFailure.ShouldBeTrue();
        TransformationEndpointSupport.FindKind("  ", NullLogger.Instance).IsFailure.ShouldBeTrue();
        TransformationEndpointSupport.FindKind("Map", NullLogger.Instance).IsFailure.ShouldBeTrue();
        TransformationEndpointSupport.FindKind("Built", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ABuiltCreateBuildsABuiltConfigurationAndAFieldMemberIsRefused()
    {
        var kind = new BuiltTransformationKind();

        var built = kind.Prepare(new CreateTransformationRequest { Name = "Total", Implementation = "Built", Inputs = Inputs(), Results = Results() }, Context());
        var refused = kind.Prepare(new CreateTransformationRequest { Name = "Total", Implementation = "Built", TransformationType = "Trim", Inputs = Inputs(), Results = Results() }, Context());

        built.IsSuccess.ShouldBeTrue();
        built.Value.ShouldBeOfType<BuiltTransformationConfiguration>().Inputs.Single().Name.ShouldBe("price");
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("transformationType");
    }

    [Fact]
    public void ABuiltCreateWithoutInputsOrResultsIsRefusedNamingWhatIsMissing()
    {
        var kind = new BuiltTransformationKind();

        var noInputs = kind.Prepare(new CreateTransformationRequest { Name = "Total", Implementation = "Built", Results = Results() }, Context());
        var noResults = kind.Prepare(new CreateTransformationRequest { Name = "Total", Implementation = "Built", Inputs = Inputs() }, Context());

        noInputs.CurrentMessage.ShouldNotBeNull().ShouldContain("inputs");
        noResults.CurrentMessage.ShouldNotBeNull().ShouldContain("results");
    }

    [Fact]
    public void AFieldCreateRefusesTheMembersOfTheBuiltKind()
    {
        var kind = new FieldTransformationKind();

        var refused = kind.Prepare(new CreateTransformationRequest { Name = "Trimmed", Implementation = "Field", TransformationType = "Trim", Inputs = Inputs() }, Context());

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("inputs");
    }

    [Fact]
    public void AFieldCreateWithoutAnOperationIsRefused()
    {
        var kind = new FieldTransformationKind();

        var refused = kind.Prepare(new CreateTransformationRequest { Name = "Trimmed", Implementation = "Field" }, Context());

        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("transformationType");
    }
}
