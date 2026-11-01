using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="StageComputeCheck"/> (FDW-1212): a Compute stage is refused where the caller reads it when its
/// transformation is missing or not a Built one, or when its input mapping does not cover exactly the declared inputs.
/// </summary>
public sealed class StageComputeCheckTests
{
    private static readonly Guid BuiltId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static StageComputeCheck Check(params ITransformationImplementationConfiguration[] known)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => Found(known.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))));
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => Found(known.FirstOrDefault(item => item.Id == id)));
        return new StageComputeCheck(provider.Object, NullLogger.Instance);
    }

    private static IGenericResult<ITransformationImplementationConfiguration> Found(ITransformationImplementationConfiguration? found) =>
        found is null
            ? GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found"))
            : GenericResult<ITransformationImplementationConfiguration>.Success(found);

    private static BuiltTransformationConfiguration Settlement() => new()
    {
        Id = BuiltId,
        Name = "Settlement",
        Implementation = BuiltTransformationConfiguration.ImplementationName,
        Inputs =
        [
            new BuiltTransformationInputConfiguration { Name = "price" },
            new BuiltTransformationInputConfiguration { Name = "quantity" },
        ],
    };

    private static PipelineCompute Stated(string? name, Guid? id, params (string Input, string Field)[] inputs) => new()
    {
        Transformation = name,
        TransformationId = id,
        Inputs = [.. inputs.Select(input => new PipelineComputeInput { InputName = input.Input, SourceField = input.Field })],
    };

    private static async Task<(Dictionary<string, Guid> Ids, List<PipelineProblem> Problems)> Run(StageComputeCheck check, PipelineCompute compute)
    {
        var ids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var problems = new List<PipelineProblem>();
        await check.Check(["Price"], "Transforms[0]", compute, ids, problems, TestContext.Current.CancellationToken);
        return (ids, problems);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANamedTransformationWithEveryInputMappedHasNoProblemAndItsIdIsFound()
    {
        // Act
        var (ids, problems) = await Run(Check(Settlement()), Stated("settlement", null, ("price", "unit_price"), ("quantity", "qty")));

        // Assert
        problems.ShouldBeEmpty();
        ids["settlement"].ShouldBe(BuiltId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATransformationGivenByIdHasNoProblem()
    {
        // Act
        var (_, problems) = await Run(Check(Settlement()), Stated(null, BuiltId, ("price", "unit_price"), ("quantity", "qty")));

        // Assert
        problems.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATransformationThatDoesNotExistIsRefused()
    {
        // Act
        var (_, problems) = await Run(Check(Settlement()), Stated("missing", null, ("price", "unit_price")));

        // Assert
        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATransformationThatIsNotBuiltIsRefused()
    {
        // Arrange
        var trim = new FieldTransformationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "Trim",
            Implementation = FieldTransformationConfiguration.ImplementationName,
            TransformationType = "Trim",
        };

        // Act
        var (_, problems) = await Run(Check(trim), Stated("Trim", null, ("price", "unit_price")));

        // Assert
        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("not a Built transformation");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInputThatNoMappingFeedsIsRefused()
    {
        // Act
        var (_, problems) = await Run(Check(Settlement()), Stated("Settlement", null, ("price", "unit_price")));

        // Assert
        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("quantity");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AMappingToAnInputTheTransformationDoesNotDeclareIsRefused()
    {
        // Act
        var (_, problems) = await Run(Check(Settlement()), Stated("Settlement", null, ("price", "a"), ("quantity", "b"), ("tax", "c")));

        // Assert
        problems.Count.ShouldBe(1);
        problems[0].Reason.ShouldContain("tax");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task NamingAndGivingTheIdTogetherIsRefused()
    {
        // Act
        var (_, problems) = await Run(Check(Settlement()), Stated("Settlement", BuiltId, ("price", "a"), ("quantity", "b")));

        // Assert
        problems.Count.ShouldBe(1);
        problems[0].Field.ShouldBe("Compute.Transformation");
    }
}
