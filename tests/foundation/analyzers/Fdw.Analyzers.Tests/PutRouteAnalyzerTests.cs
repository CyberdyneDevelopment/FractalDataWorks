using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// FDW058 (FDW-4125): an endpoint never declares PUT. Mike, 2026-11-05: "we always always always use post and patch
/// definitely not put".
/// </summary>
public class PutRouteAnalyzerTests
{
    // The FastEndpoints members the rule reads: the verb methods on an endpoint and the Http enum. Appended after each
    // snippet, never before it: a snippet opens with its usings, which must precede every namespace in the file (CS1529).
    private const string Stubs = @"
namespace FastEndpoints
{
    public enum Http { GET, POST, PUT, PATCH, DELETE }

    public abstract class BaseEndpoint
    {
        protected void Get(params string[] routes) { }
        protected void Post(params string[] routes) { }
        protected void Put(params string[] routes) { }
        protected void Patch(params string[] routes) { }
        protected void Delete(params string[] routes) { }
        protected void Verbs(params Http[] methods) { }
        protected void Verbs(params string[] methods) { }
        protected void Routes(params string[] routes) { }
    }
}
";

    private static Task Run(string source)
    {
        var test = new CSharpAnalyzerTest<PutRouteAnalyzer, DefaultVerifier>
        {
            TestCode = source + Stubs,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task PostPatchGetAndDeleteAreSilent() => Run(@"
using FastEndpoints;

class Endpoints : BaseEndpoint
{
    void Configure()
    {
        Get(""/things/{Name}"");
        Post(""/things"");
        Patch(""/things/{Name}"");
        Delete(""/things/{Name}"");
        Verbs(Http.PATCH);
        Verbs(""POST"", ""PATCH"");
    }
}
");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task APutRouteReports() => Run(@"
using FastEndpoints;

class Endpoints : BaseEndpoint
{
    void Configure()
    {
        {|FDW058:Put(""/things/{Name}"")|};
    }
}
");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task VerbsNamingPutReports() => Run(@"
using FastEndpoints;

class Endpoints : BaseEndpoint
{
    void ByEnum()
    {
        Verbs({|FDW058:Http.PUT|});
    }

    void ByText()
    {
        Verbs(""PATCH"", {|FDW058:""put""|});
    }
}
");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public Task APutVerbStoredForLaterReports() => Run(@"
using FastEndpoints;

sealed record Relay(Http Verb, string Route);

class Endpoints : BaseEndpoint
{
    private static readonly Relay Stored = new({|FDW058:Http.PUT|}, ""/etl/things"");

    void Configure()
    {
        Verbs(Stored.Verb);
    }
}
");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public Task OutboundHttpPutIsSilent() => Run(@"
using System.Net.Http;

class Outbound
{
    HttpRequestMessage Update() => new(HttpMethod.Put, ""https://other.example/things/1"");

    void Local()
    {
        Put(""/not/fastendpoints"");
    }

    void Put(string route) { }
}
");
}
