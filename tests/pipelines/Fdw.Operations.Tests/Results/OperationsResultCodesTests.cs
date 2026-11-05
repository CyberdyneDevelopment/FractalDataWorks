using System.Linq;
using Fdw.Operations.Abstractions.Results;

namespace Fdw.Operations.Tests.Results;

/// <summary>Tests for the Operations result codes (FDW-4037): the collection looks codes up by Id, so no two may report the same one.</summary>
public sealed class OperationsResultCodesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NoTwoResultCodesReportTheSameId()
    {
        var shared = OperationsResultCodes.All()
            .GroupBy(code => code.Id)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(code => code.Name))}")
            .ToList();

        shared.ShouldBeEmpty();
    }
}
