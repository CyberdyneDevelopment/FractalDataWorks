using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

// FDW-1354: a nearest search is built only when every part is given; a refusal names the part.
public sealed class NearestCommandBuilderTests
{
    private static NearestCommandBuilder<object> Whole() =>
        Vectorized.Nearest<object>("TechComDb", "tc", "ConversationPassage")
            .To("Vector", [1f, 0f, 0f])
            .Metric(VectorMetrics.Cosine)
            .Take(10);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildsTheSearchAndItsTarget()
    {
        var result = Whole().Build();

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var call = result.Value;
        call.Target.DataStore.ShouldBe("TechComDb");
        call.Target.Container.ShouldBe("ConversationPassage");
        var command = call.Command.ShouldBeOfType<NearestCommand<object>>();
        command.VectorField.ShouldBe("Vector");
        command.Vector.ShouldBe(new[] { 1f, 0f, 0f });
        command.Metric.ShouldBeSameAs(VectorMetrics.Cosine);
        command.Take.ShouldBe(10);
        command.CommandType.ShouldBe("Nearest");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RefusesASearchWithNoVectorField()
    {
        var result = Vectorized.Nearest<object>("TechComDb", "tc", "ConversationPassage")
            .Metric(VectorMetrics.Cosine).Take(10).Build();

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("VectorField");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RefusesASearchWithNoMetric()
    {
        var result = Vectorized.Nearest<object>("TechComDb", "tc", "ConversationPassage")
            .To("Vector", [1f]).Take(10).Build();

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Metric");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RefusesAVectorWithNoNumbers()
    {
        var result = Whole().To("Vector", []).Build();

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Vector");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RefusesASearchForNoRows()
    {
        var result = Whole().Take(0).Build();

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Take");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RefusesASearchWithNoContainer()
    {
        var result = Vectorized.Nearest<object>("TechComDb", "tc", "")
            .To("Vector", [1f]).Metric(VectorMetrics.Dot).Take(1).Build();

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("ContainerName");
    }
}
