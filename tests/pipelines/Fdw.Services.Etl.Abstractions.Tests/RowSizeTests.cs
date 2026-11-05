using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions.Flow;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Abstractions.Tests;

/// <summary>
/// Tests for the size estimate of rows that move over a link (FDW-3381): text two bytes a character, a number its width, a date eight bytes, nothing for a null, and a fixed
/// size for a type the rule does not list.
/// </summary>
public sealed class RowSizeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueCountsByItsType()
    {
        RowSize.Of("abc").ShouldBe(6);
        RowSize.Of((object?)null).ShouldBe(0);
        RowSize.Of(true).ShouldBe(1);
        RowSize.Of((short)1).ShouldBe(2);
        RowSize.Of(1).ShouldBe(4);
        RowSize.Of(1L).ShouldBe(8);
        RowSize.Of(1.5).ShouldBe(8);
        RowSize.Of(new DateTime(2026, 1, 1)).ShouldBe(8);
        RowSize.Of(1.5m).ShouldBe(16);
        RowSize.Of(Guid.Empty).ShouldBe(16);
        RowSize.Of(new byte[10]).ShouldBe(10);
        RowSize.Of(new object()).ShouldBe(RowSize.OtherValue);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARowCountsItsValuesAndNotItsColumnNames()
    {
        IDictionary<string, object?> row = new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "ab", ["Missing"] = null };

        RowSize.Of(row).ShouldBe(8);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ABatchCountsEveryRow()
    {
        var batch = new FlowBatch([
            new Dictionary<string, object?> { ["Id"] = 1 },
            new Dictionary<string, object?> { ["Id"] = 2 },
            new Dictionary<string, object?> { ["Id"] = 3 },
        ]);

        RowSize.Of(batch).ShouldBe(12);
        RowSize.Of(new FlowBatch([])).ShouldBe(0);
    }
}
