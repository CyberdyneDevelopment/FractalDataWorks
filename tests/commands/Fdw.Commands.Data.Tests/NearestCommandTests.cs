using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

// FDW-1354: the command builds its own answer from the rows a connection read, so the connection needs no knowledge of it.
public sealed class NearestCommandTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResultsPairEachRowWithItsDistanceInTheOrderRead()
    {
        var command = new NearestCommand<string>
        {
            VectorField = "Vector",
            Vector = [1f],
            Metric = VectorMetrics.Cosine,
            Take = 2,
        };

        var results = command.Results(["near", "far"], [0.1, 0.9]).ShouldBeAssignableTo<IReadOnlyList<NearestResult<string>>>();

        results.ShouldNotBeNull();
        results.Count.ShouldBe(2);
        results[0].Record.ShouldBe("near");
        results[0].Distance.ShouldBe(0.1);
        results[1].Record.ShouldBe("far");
        results[1].Distance.ShouldBe(0.9);
        command.RowType.ShouldBe(typeof(string));
    }
}
