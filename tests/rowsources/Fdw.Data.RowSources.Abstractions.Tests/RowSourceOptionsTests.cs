using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RowSourceOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowsDefaultsToZero()
    {
        var sut = new RowSourceOptions() { ContinueOnError = true, BufferSize = 16 * 1024 };

        sut.MaxRows.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowErrorsDefaultsToZero()
    {
        var sut = new RowSourceOptions() { ContinueOnError = true, BufferSize = 16 * 1024 };

        sut.MaxRowErrors.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BufferSizeCanBeSet()
    {
        var sut = new RowSourceOptions { BufferSize = 4096, ContinueOnError = true };

        sut.BufferSize.ShouldBe(4096);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowsCanBeSet()
    {
        var sut = new RowSourceOptions { MaxRows = 1000, ContinueOnError = true, BufferSize = 16 * 1024 };

        sut.MaxRows.ShouldBe(1000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ContinueOnErrorCanBeDisabled()
    {
        var sut = new RowSourceOptions { ContinueOnError = false, BufferSize = 16 * 1024 };

        sut.ContinueOnError.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MaxRowErrorsCanBeSet()
    {
        var sut = new RowSourceOptions { MaxRowErrors = 50, ContinueOnError = true, BufferSize = 16 * 1024 };

        sut.MaxRowErrors.ShouldBe(50);
    }
}
