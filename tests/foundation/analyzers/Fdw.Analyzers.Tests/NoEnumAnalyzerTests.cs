using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// FDW059 (FDW-4154, Feature FDW-4143): no enum is declared and none is parsed. Mike, 2026-11-05: enums are "expressly
/// forbidden in fdw", "replace all of those other than messageseverity with type collections", and "the roslyn ones are an
/// allowable exception".
/// </summary>
public class NoEnumAnalyzerTests
{
    private static CSharpAnalyzerTest<NoEnumAnalyzer, DefaultVerifier> Test(string source, string? path = null, string? assemblyName = null)
    {
        var test = new CSharpAnalyzerTest<NoEnumAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        if (path is null)
            test.TestCode = source;
        else
            test.TestState.Sources.Add((path, source));

        if (assemblyName is not null)
            test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectAssemblyName(projectId, assemblyName));

        return test;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task AnEnumDeclarationReports() => Test(@"
namespace App
{
    public enum {|FDW059:RunState|} { Waiting, Running }

    public class Holder
    {
        private enum {|FDW059:Phase|} { First, Second }
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task MessageSeverityIsSilent() => Test(@"
namespace Fdw.Messages
{
    public enum MessageSeverity { Information, Warning, Error }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task EnumParseAndTryParseReport() => Test(@"
namespace App
{
    public class Reader
    {
        public object Generic() => {|FDW059:System.Enum.Parse<System.DayOfWeek>(""Monday"")|};

        public object ByType() => {|FDW059:System.Enum.Parse(typeof(System.DayOfWeek), ""Monday"")|};

        public bool Try(string text) => {|FDW059:System.Enum.TryParse<System.DayOfWeek>(text, true, out _)|};
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public Task UsingALibraryEnumWhereTheLibraryIsCalledIsSilent() => Test(@"
namespace App
{
    public class Boundary
    {
        public string Read(string name) => System.Environment.GetEnvironmentVariable(name, System.EnvironmentVariableTarget.Process);

        public bool IsWeekend(System.DayOfWeek day) => day == System.DayOfWeek.Saturday || day == System.DayOfWeek.Sunday;

        public string Name(System.DayOfWeek day) => day.ToString();
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public Task RoslynToolingIsExempt() => Test(@"
namespace Fdw.Roslyn.Commands
{
    public enum AuditFindingKind { Missing, Extra }

    public class Reader
    {
        public object Read(string text) => System.Enum.Parse<AuditFindingKind>(text);
    }
}
", path: "/repo/public/src/tooling/roslyn/Fdw.Roslyn.Commands/AuditFindingKind.cs").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public Task ASourceGeneratorAssemblyIsExempt() => Test(@"
namespace Fdw.Collections.SourceGenerators
{
    internal enum CollectionKind { TypeCollection, ServiceTypeCollection }
}
", assemblyName: "Fdw.Collections.SourceGenerators").RunAsync(TestContext.Current.CancellationToken);
}
