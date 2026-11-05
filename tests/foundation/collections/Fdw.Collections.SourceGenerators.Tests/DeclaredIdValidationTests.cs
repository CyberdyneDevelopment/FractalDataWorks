using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// Two options of one collection may not declare the same Id (TC008), and an Id the generator cannot read is named (TC013) (FDW-4045).
/// </summary>
/// <remarks>
/// Why through a chain of bases: a result code passes its number to a domain base, which passes it on under another parameter name
/// to a base compiled into another assembly. TypeOptionBase reaches these compilations as metadata, so each case also proves the
/// [TypeOptionId] mark is read there.
/// </remarks>
public sealed class DeclaredIdValidationTests
{
    private const string Header = @"
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Test;

public abstract class TestBase : TypeOptionBase<int, TestBase>
{
    protected TestBase(int number, string name) : base(number, name) { }
}

public abstract class DomainBase : TestBase
{
    protected DomainBase(string name, int number) : base(number, name) { }
}

[TypeCollection(typeof(TestBase), typeof(TestBase), typeof(Tests))]
public partial class Tests : TypeCollectionBase<TestBase, TestBase>
{
}
";

    private static string[] Ids(string source, string diagnosticId) =>
        [.. CompilationHelper.RunGenerator(Header + source).Diagnostics
            .Where(diagnostic => string.Equals(diagnostic.Id, diagnosticId, StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture))];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void TwoOptionsDeclaringOneIdThroughTwoBasesAreRefused()
    {
        var refused = Ids(@"
[TypeOption(typeof(Tests), ""First"")]
public sealed class First : DomainBase { public First() : base(""First"", 20010) { } }

[TypeOption(typeof(Tests), ""Second"")]
public sealed class Second : DomainBase { public Second() : base(""Second"", 20010) { } }
", "TC008");

        refused.Length.ShouldBe(1);
        refused[0].ShouldContain("Id = '20010'");
        refused[0].ShouldContain("Test.First");
        refused[0].ShouldContain("Test.Second");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AnIdPassedByNamedArgumentOrConstIsRead()
    {
        var refused = Ids(@"
public static class Numbers { public const int Shared = 7; }

[TypeOption(typeof(Tests), ""First"")]
public sealed class First : DomainBase { public First() : base(number: Numbers.Shared, name: ""First"") { } }

[TypeOption(typeof(Tests), ""Second"")]
public sealed class Second : TestBase { public Second() : base(7, ""Second"") { } }
", "TC008");

        refused.Length.ShouldBe(1);
        refused[0].ShouldContain("Id = '7'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void OptionsDeclaringDifferentIdsPass()
    {
        const string source = @"
[TypeOption(typeof(Tests), ""First"")]
public sealed class First : DomainBase { public First() : base(""First"", 1) { } }

[TypeOption(typeof(Tests), ""Second"")]
public sealed class Second : DomainBase { public Second() : base(""Second"", 2) { } }
";

        Ids(source, "TC008").ShouldBeEmpty();
        Ids(source, "TC013").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void AnIdComputedAtRunTimeIsNamedAsUnread()
    {
        var unread = Ids(@"
public static class Numbers { public static int Next() => 3; }

[TypeOption(typeof(Tests), ""First"")]
public sealed class First : DomainBase { public First() : base(""First"", Numbers.Next()) { } }
", "TC013");

        unread.Length.ShouldBe(1);
        unread[0].ShouldContain("Test.First");
        unread[0].ShouldContain("not a constant");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AnUnreadIdIsNotCountedAsADuplicate()
    {
        var refused = Ids(@"
public static class Numbers { public static int Next() => 3; }

[TypeOption(typeof(Tests), ""First"")]
public sealed class First : DomainBase { public First() : base(""First"", Numbers.Next()) { } }

[TypeOption(typeof(Tests), ""Second"")]
public sealed class Second : DomainBase { public Second() : base(""Second"", Numbers.Next()) { } }
", "TC008");

        refused.ShouldBeEmpty();
    }
}
