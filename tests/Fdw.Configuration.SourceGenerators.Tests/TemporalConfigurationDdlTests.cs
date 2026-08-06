using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Configuration.SourceGenerators.Tests;

/// <summary>
/// Covers the opt-in valid-time (effective dating) DDL emitted for
/// <c>[ManagedConfiguration(Temporal = true)]</c>.
/// </summary>
/// <remarks>
/// The opt-out case matters as much as the opt-in one: valid-time is meaningful only where the
/// period a record GOVERNS differs from when it was written. Emitting the columns everywhere would
/// impose bitemporal semantics — and a nullable interval every read has to reason about — on the
/// ~200 configuration tables whose only meaningful version is the current one.
/// </remarks>
public class TemporalConfigurationDdlTests
{
    private const string TemporalSource = @"
using Fdw.Configuration;

namespace Test
{
    [ManagedConfiguration(Temporal = true)]
    public partial class RateConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}";

    private const string NonTemporalSource = @"
using Fdw.Configuration;

namespace Test
{
    [ManagedConfiguration]
    public partial class RateConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TemporalConfigurationEmitsEffectiveStartAndEnd()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(TemporalSource);

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RateConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl!.ShouldContain("Name = \"EffectiveStart\"");
        ddl.ShouldContain("Name = \"EffectiveEnd\"");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void EffectiveEndIsNullableSoAnOpenEndedVersionNeedsNoSentinel()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(TemporalSource);
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RateConfiguration.Ddl.g.cs");

        // The currently-in-force version has no known end. A sentinel far-future date would make
        // every as-of predicate depend on that magic value being written correctly on every insert.
        var effectiveEndBlock = ddl!.Substring(ddl.IndexOf("Name = \"EffectiveEnd\"", System.StringComparison.Ordinal));
        effectiveEndBlock.ShouldContain("IsNullable = true");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TemporalConfigurationEmitsAnAsOfLookupIndex()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(TemporalSource);
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RateConfiguration.Ddl.g.cs");

        // Why this index is not optional: every other index on these tables is filtered
        // WHERE IsCurrent = 1, so none of them serves a historical lookup — an as-of read would
        // scan the entity's entire version history on every restatement.
        ddl.ShouldNotBeNull();
        ddl!.ShouldContain("IX_Rate_Id_Effective");
        ddl!.ShouldContain("new[] { \"Id\", \"EffectiveStart\", \"EffectiveEnd\" }");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void NonTemporalConfigurationEmitsNoEffectiveColumns()
    {
        var (compilation, diagnostics) = CompilationHelper.RunGenerator(NonTemporalSource);

        diagnostics.ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);

        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RateConfiguration.Ddl.g.cs");
        ddl.ShouldNotBeNull();
        ddl!.ShouldNotContain("Effective");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TemporalConfigurationKeepsTransactionTimeAuditColumns()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(TemporalSource);
        var ddl = CompilationHelper.GetGeneratedOutput(compilation, "RateConfiguration.Ddl.g.cs");

        // Valid-time ADDS to transaction-time, it does not replace it. "What was in force for
        // period X" and "what did we believe on date X" are different questions, and an audit of
        // who changed what still needs the second.
        ddl.ShouldNotBeNull();
        ddl!.ShouldContain("Name = \"CreateDate\"");
        ddl!.ShouldContain("Name = \"ModifyDate\"");
        ddl!.ShouldContain("Name = \"IsCurrent\"");
    }
}
