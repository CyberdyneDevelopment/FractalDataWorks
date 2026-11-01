using System;
using System.Linq;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Validation;

namespace Fdw.Services.Etl.Projects.Tests.Validation;

/// <summary>
/// Tests for the save-time prerequisite rules of <see cref="OrchestrationNodeConfigurationValidator"/>: both pipelines of a
/// prerequisite belong to the node and the prerequisites form no cycle (FDW-2083).
/// </summary>
public sealed class OrchestrationNodeConfigurationValidatorPrerequisiteTests
{
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-00000000000c");

    private readonly OrchestrationNodeConfigurationValidator _sut = new(Mock.Of<IServerPolicyDefaults>());

    private static OrchestrationNodeImplementationConfiguration Step(
        Guid[] members, (Guid Pipeline, Guid Needs)[] prerequisites) => new()
    {
        Name = "Load sales",
        NodeTypeId = OrchestrationNodeTypes.ByName("Step").Id,
        ParentId = Guid.NewGuid(),
        PipelineMemberships = members.Select((id, i) => new OrchestrationNodePipelineMembershipConfiguration { PipelineId = id, Name = NameOf(id), Ordinal = i }).ToList(),
        PipelinePrerequisites = prerequisites.Select(p => new OrchestrationNodePipelinePrerequisiteConfiguration
        {
            PipelineId = p.Pipeline,
            PrerequisitePipelineId = p.Needs,
            Name = $"{NameOf(p.Pipeline)} needs {NameOf(p.Needs)}"
        }).ToList()
    };

    private static string NameOf(Guid id) => id == A ? "Sales" : id == B ? "Budget" : "Forecast";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void PrerequisitesBetweenMembersWithNoCycleAreAccepted()
    {
        // Act
        var result = _sut.Validate(Step([A, B, C], [(C, A), (C, B)]));

        // Assert
        result.IsValid.ShouldBeTrue(string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void PrerequisiteOnAPipelineOutsideTheNodeIsRefused()
    {
        // Act
        var result = _sut.Validate(Step([A, B], [(B, C)]));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorMessage.Contains("Budget needs Forecast", StringComparison.Ordinal) &&
            e.ErrorMessage.Contains("does not hold", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void PipelineThatNeedsItselfIsRefused()
    {
        // Act
        var result = _sut.Validate(Step([A], [(A, A)]));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("cycle", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ALoopIsNamedByAPipelineInItNotOneThatOnlyDependsOnIt()
    {
        // Arrange: C needs A, and A and B need each other. C is listed first but is not in the loop.
        var step = Step([C, A, B], [(A, B), (B, A), (C, A)]);

        // Act
        var result = _sut.Validate(step);

        // Assert
        result.IsValid.ShouldBeFalse();
        var cycle = result.Errors.Single(e => e.ErrorMessage.Contains("cycle", StringComparison.Ordinal)).ErrorMessage;
        cycle.ShouldNotContain("Forecast");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void PrerequisitesThatLoopAreRefusedNamingAPipelineInTheLoop()
    {
        // Act
        var result = _sut.Validate(Step([A, B, C], [(A, B), (B, A), (C, A)]));

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.ErrorMessage.Contains("cycle", StringComparison.Ordinal) &&
            e.ErrorMessage.Contains("'Sales'", StringComparison.Ordinal));
    }
}
