using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// FDW-4171: a collection whose options carry a type from a namespace that shares its first segment with the collection's
/// own (Serilog.Events.LogEventLevel inside FdwReferenceLogging.Serilog, FDW-4153) compiles. The generator writes the
/// carried type fully qualified, so <c>Logging.Events.Level</c> named inside <c>Acme.Logging.Levels</c> no longer resolves to
/// <c>Acme.Logging</c> (CS0234), in the lookup method, its dictionary, and the NotFound sentinel's constructor call.
/// </summary>
public class CarriedTypeQualificationTests
{
    private const string Source = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Logging.Events
{
    public enum Level { Low, High }
}

namespace Acme.Logging.Levels
{
    public abstract class LevelOptionBase : TypeOptionBase<int, LevelOptionBase>
    {
        protected LevelOptionBase(int id, string name, global::Logging.Events.Level level) : base(id, name)
        {
            Level = level;
        }

        [TypeLookup(""ByLevel"")]
        public global::Logging.Events.Level Level { get; }
    }

    [TypeCollection(typeof(LevelOptionBase), typeof(LevelOptionBase), typeof(LevelOptions))]
    public partial class LevelOptions : TypeCollectionBase<LevelOptionBase, LevelOptionBase>
    {
    }

    [TypeOption(typeof(LevelOptions), ""Low"")]
    public sealed class LowOption : LevelOptionBase
    {
        public LowOption() : base(1, ""Low"", global::Logging.Events.Level.Low) { }
    }

    [TypeOption(typeof(LevelOptions), ""High"")]
    public sealed class HighOption : LevelOptionBase
    {
        public HighOption() : base(2, ""High"", global::Logging.Events.Level.High) { }
    }
}
";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void ACarriedTypeWhoseNamespaceSharesTheCollectionsFirstSegmentCompiles()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(Source);

        // Why only the errors that name Logging: this harness compiles without System.Collections.Immutable and Fdw.Types, so the
        // generated FrozenDictionary and Fdw.Types code reports its own CS0234/CS0246 here (the real build has those references).
        // The bug is the carried type resolving into Acme.Logging ("'Events' does not exist in the namespace 'Acme.Logging'"), and
        // every error it causes names Logging; the harness's own errors do not.
        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error && d.Id is "CS0234" or "CS0246" or "CS0103")
            .Select(d => $"{d.Id}: {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}")
            .Where(message => message.Contains("Logging", System.StringComparison.Ordinal))
            .ToArray();

        errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void TheLookupMethodNamesTheCarriedTypeFromGlobal()
    {
        var (compilation, _) = CompilationHelper.RunGenerator(Source);

        var generated = string.Join("\n", compilation.SyntaxTrees.Select(tree => tree.ToString()));

        generated.ShouldContain("ByLevel(global::Logging.Events.Level value)");
    }
}
