using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Refactoring.Results;

namespace Fdw.Roslyn.Commands.Refactoring.Tests;

/// <summary>
/// Tests for <see cref="Fdw.Roslyn.Commands.Refactoring.Translators.ResolveInheritDocTranslator"/>.
/// </summary>
public sealed class ResolveInheritDocTranslatorTests
{
    [Fact]
    public async Task InterfaceImplementation_ExpandsInheritedTags()
    {
        const string source = @"
public interface IFoo
{
    /// <summary>Does foo.</summary>
    /// <param name=""id"">The identifier.</param>
    /// <returns>A value.</returns>
    int Foo(int id);
}
public class Bar : IFoo
{
    /// <inheritdoc/>
    public int Foo(int id) => id;
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.Data.SitesUnresolved.ShouldBe(0);
        run.NewText.ShouldNotContain("<inheritdoc");
        run.NewText.ShouldContain("/// <summary>Does foo.</summary>");
        run.NewText.ShouldContain("/// <param name=\"id\">The identifier.</param>");
        run.NewText.ShouldContain("/// <returns>A value.</returns>");
    }

    [Fact]
    public async Task Override_ExpandsBaseSummary()
    {
        const string source = @"
public abstract class Base
{
    /// <summary>Base summary.</summary>
    public virtual void M() {}
}
public class Derived : Base
{
    /// <inheritdoc/>
    public override void M() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.NewText.ShouldContain("/// <summary>Base summary.</summary>");
        run.NewText.ShouldNotContain("<inheritdoc");
    }

    [Fact]
    public async Task GenericParameterSubstitution_InsertsResolvedParamVerbatim()
    {
        const string source = @"
public interface IRepo<T>
{
    /// <summary>Gets by id.</summary>
    /// <param name=""id"">The id of type T.</param>
    T Get(T id);
}
public class IntRepo : IRepo<int>
{
    /// <inheritdoc/>
    public int Get(int id) => id;
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.NewText.ShouldContain("/// <summary>Gets by id.</summary>");
        run.NewText.ShouldContain("/// <param name=\"id\">The id of type T.</param>");
    }

    [Fact]
    public async Task CrefForm_ResolvesTargetDocs()
    {
        const string source = @"
public class Source
{
    /// <summary>From source.</summary>
    public void Thing() {}
}
public class Consumer
{
    /// <inheritdoc cref=""Source.Thing""/>
    public void Other() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.NewText.ShouldContain("/// <summary>From source.</summary>");
        run.NewText.ShouldNotContain("<inheritdoc");
    }

    [Fact]
    public async Task NoBaseMember_ReportsUnresolvedAndLeavesSourceUntouched()
    {
        const string source = @"
public class Lonely
{
    /// <inheritdoc/>
    public void Solo() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(0);
        run.Data.SitesUnresolved.ShouldBe(1);
        run.Data.Unresolved.Single().Reason.ShouldBe(UnresolvedReason.NoBaseMember);
        run.Data.Unresolved.Single().SymbolDisplayName.ShouldContain("Solo");
        // Why: unresolved sites are the user's intent — never delete them.
        run.NewText.ShouldContain("/// <inheritdoc/>");
    }

    [Fact]
    public async Task UnresolvableCref_ReportsCrefTargetNotFound()
    {
        const string source = @"
public class Consumer
{
    /// <inheritdoc cref=""DoesNotExist.Nope""/>
    public void Other() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesUnresolved.ShouldBe(1);
        run.Data.Unresolved.Single().Reason.ShouldBe(UnresolvedReason.CrefTargetNotFound);
        run.NewText.ShouldContain("<inheritdoc");
    }

    [Fact]
    public async Task MultiLineResolvedContent_PreservesLeadingSlashesAndIndentation()
    {
        // The implementing member is indented 8 spaces; every emitted line must carry that exact prefix.
        const string source =
            "public interface IMulti\n" +
            "{\n" +
            "    /// <summary>\n" +
            "    /// First line.\n" +
            "    /// Second line.\n" +
            "    /// </summary>\n" +
            "    void Do();\n" +
            "}\n" +
            "public class Impl : IMulti\n" +
            "{\n" +
            "        /// <inheritdoc/>\n" +
            "        public void Do() {}\n" +
            "}\n";

        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.NewText.ShouldContain("        /// <summary>");
        run.NewText.ShouldContain("        /// First line.");
        run.NewText.ShouldContain("        /// Second line.");
        run.NewText.ShouldContain("        /// </summary>");
        run.NewText.ShouldNotContain("<inheritdoc");
    }

    [Fact]
    public async Task SiblingTag_IsPreserved()
    {
        const string source = @"
public interface IFoo
{
    /// <summary>Does foo.</summary>
    /// <param name=""id"">The identifier.</param>
    /// <returns>A value.</returns>
    int Foo(int id);
}
public class Bar : IFoo
{
    /// <inheritdoc/>
    /// <remarks>Extra note.</remarks>
    public int Foo(int id) => id;
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(1);
        run.NewText.ShouldContain("/// <summary>Does foo.</summary>");
        run.NewText.ShouldContain("/// <remarks>Extra note.</remarks>");
        run.NewText.ShouldNotContain("<inheritdoc");
    }

    [Fact]
    public async Task Idempotent_SecondRunIsNoOp()
    {
        const string source = @"
public interface IFoo
{
    /// <summary>Does foo.</summary>
    /// <param name=""id"">The identifier.</param>
    /// <returns>A value.</returns>
    int Foo(int id);
}
public class Bar : IFoo
{
    /// <inheritdoc/>
    public int Foo(int id) => id;
}
";
        var first = await InheritDocTestHarness.RunAsync(source);
        first.Data!.SitesResolved.ShouldBe(1);

        var second = await InheritDocTestHarness.RunAsync(first.NewText);
        second.Data!.SitesResolved.ShouldBe(0);
        second.Data.SitesUnresolved.ShouldBe(0);
        second.NewText.ShouldBe(first.NewText);
    }

    [Fact]
    public async Task RecursiveChain_ResolvesThroughIntermediate()
    {
        // C documents Run; B overrides with only <inheritdoc/>; A overrides with only <inheritdoc/>.
        // Both A.Run and B.Run must resolve to C's docs by walking the override chain.
        const string source = @"
public abstract class C
{
    /// <summary>Root docs.</summary>
    public virtual void Run() {}
}
public abstract class B : C
{
    /// <inheritdoc/>
    public override void Run() {}
}
public class A : B
{
    /// <inheritdoc/>
    public override void Run() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.SitesResolved.ShouldBe(2);
        run.Data.SitesUnresolved.ShouldBe(0);
        run.NewText.ShouldNotContain("<inheritdoc");
        run.NewText.ShouldContain("/// <summary>Root docs.</summary>");
    }

    [Fact]
    public async Task FileScope_NonexistentFile_Fails()
    {
        const string source = "public class X {}";

        var run = await InheritDocTestHarness.RunAsync(source, filePath: "/virtual/Missing.cs");

        run.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task FileScope_MatchingFile_ProcessesDocument()
    {
        const string source = @"
public abstract class Base
{
    /// <summary>Base summary.</summary>
    public virtual void M() {}
}
public class Derived : Base
{
    /// <inheritdoc/>
    public override void M() {}
}
";
        var run = await InheritDocTestHarness.RunAsync(source, filePath: InheritDocTestHarness.DocPath);

        run.IsSuccess.ShouldBeTrue();
        run.Data!.FilesScanned.ShouldBe(1);
        run.Data.SitesResolved.ShouldBe(1);
    }
}
