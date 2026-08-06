using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RowSourceTypeBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        var sut = new TestRowSourceType();

        sut.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        var sut = new TestRowSourceType();

        sut.Name.ShouldBe("TestSource");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsSyncReturnsConfiguredValue()
    {
        var sut = new TestRowSourceType();

        sut.SupportsSync.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsAsyncReturnsConfiguredValue()
    {
        var sut = new TestRowSourceType();

        sut.SupportsAsync.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsResetReturnsConfiguredValue()
    {
        var sut = new TestRowSourceType();

        sut.SupportsReset.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TypicalAllocationsPerRowReturnsConfiguredValue()
    {
        var sut = new TestRowSourceType();

        sut.TypicalAllocationsPerRow.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatReturnsConfiguredValue()
    {
        var sut = new TestRowSourceType();

        sut.Format.ShouldBe("TestFormat");
    }

    private sealed class TestRowSourceType : RecordSourceTypeBase
    {
        public TestRowSourceType() : base(1, "TestSource") { }

        public override bool SupportsSync => true;
        public override bool SupportsAsync => true;
        public override bool SupportsReset => false;
        public override int TypicalAllocationsPerRow => 2;
        public override string Format => "TestFormat";

        // Why: test double — the base now requires a CreateReader factory; this fixture exercises the
        // metadata properties, not reading, so the factory throws to signal it isn't the unit under test.
        public override IRowSourceReader CreateReader(System.IO.Stream content, RowSourceOptions? options)
            => throw new System.NotSupportedException("Test fixture does not create readers.");

        // Why: the base also requires the config-driven Create surface; same test-double rationale.
        public override IRecordSource<DataRecord> Create(RecordSourceContext context)
            => throw new System.NotSupportedException("Test fixture does not create record sources.");
    }
}
