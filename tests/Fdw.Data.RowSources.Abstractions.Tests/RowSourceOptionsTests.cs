using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RowSourceOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BufferSizeDefaultsTo16K()
    {
        var sut = new RowSourceOptions();

        sut.BufferSize.ShouldBe(16 * 1024);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowsDefaultsToZero()
    {
        var sut = new RowSourceOptions();

        sut.MaxRows.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContinueOnErrorDefaultsToTrue()
    {
        var sut = new RowSourceOptions();

        sut.ContinueOnError.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowErrorsDefaultsToZero()
    {
        var sut = new RowSourceOptions();

        sut.MaxRowErrors.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BufferSizeCanBeSet()
    {
        var sut = new RowSourceOptions { BufferSize = 4096 };

        sut.BufferSize.ShouldBe(4096);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowsCanBeSet()
    {
        var sut = new RowSourceOptions { MaxRows = 1000 };

        sut.MaxRows.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContinueOnErrorCanBeDisabled()
    {
        var sut = new RowSourceOptions { ContinueOnError = false };

        sut.ContinueOnError.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowErrorsCanBeSet()
    {
        var sut = new RowSourceOptions { MaxRowErrors = 50 };

        sut.MaxRowErrors.ShouldBe(50);
    }
}
