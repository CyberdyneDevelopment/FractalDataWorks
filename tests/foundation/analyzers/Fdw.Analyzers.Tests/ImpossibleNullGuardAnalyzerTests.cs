using System.Threading.Tasks;
using Fdw.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Analyzers.Tests;

/// <summary>
/// FDW063 (FDW-5198, FDW-5184): an ArgumentNullException guard on a parameter that cannot be null is reported and removed.
/// FDW does not throw ArgumentNullException, and with nullable enabled a non-nullable parameter already takes no null.
/// </summary>
public class ImpossibleNullGuardAnalyzerTests
{
    private static CSharpAnalyzerTest<ImpossibleNullGuardAnalyzer, DefaultVerifier> Analyze(string source) => new()
    {
        TestCode = source,
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
    };

    private static CSharpCodeFixTest<ImpossibleNullGuardAnalyzer, ImpossibleNullGuardCodeFixProvider, DefaultVerifier> Fix(string source, string fixedSource) => new()
    {
        TestCode = source,
        FixedCode = fixedSource,
        ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task EachGuardShapeOnANonNullableParameterReports() => Analyze(@"
#nullable enable
using System;

public class C<TClass> where TClass : class
{
    private string _name = string.Empty;

    public void Statement(string name)
    {
        {|FDW063:if (name == null) throw new ArgumentNullException(nameof(name));|}
        _name = name;
    }

    public void Reversed(string name)
    {
        {|FDW063:if (null == name) throw new ArgumentNullException(nameof(name));|}
        _name = name;
    }

    public void Pattern(string name)
    {
        {|FDW063:if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }|}

        _name = name;
    }

    public void Coalesce(string name) => _name = {|FDW063:name ?? throw new ArgumentNullException(nameof(name))|};

    public void ThrowIfNull(string name)
    {
        {|FDW063:ArgumentNullException.ThrowIfNull(name);|}
        _name = name;
    }

    public void ClassConstrained(TClass value)
    {
        {|FDW063:ArgumentNullException.ThrowIfNull(value);|}
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task AValueThatMayBeNullOrAGuardTheFixCannotRemoveIsSilent() => Analyze(@"
#nullable enable
using System;

public class C<T>
{
    private string _field = string.Empty;

    public void NullableReference(string? a) { if (a == null) throw new ArgumentNullException(nameof(a)); }

    public void NullableValue(int? b) { if (b == null) throw new ArgumentNullException(nameof(b)); }

    public void UnconstrainedGeneric(T c) { ArgumentNullException.ThrowIfNull(c); }

    public void Field() { if (_field == null) throw new ArgumentNullException(nameof(_field)); }

    public void Compound(string d, string e) { if (d == null || e == null) throw new ArgumentNullException(nameof(d)); }

    public void EmptyIsAlsoRefused(string f) { ArgumentException.ThrowIfNullOrEmpty(f); }

    public void OtherException(string g) { if (g == null) throw new InvalidOperationException(); }

    public void WithElse(string h) { if (h == null) throw new ArgumentNullException(nameof(h)); else Console.WriteLine(h); }
}

#nullable disable
public class Oblivious
{
    public void Run(string i) { if (i == null) throw new ArgumentNullException(nameof(i)); }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task TheFixRemovesAGuardingIfAndTheBlankLineItLeaves() => Fix(@"
#nullable enable
using System;

public class C
{
    private readonly string _name;

    public C(string name)
    {
        {|FDW063:if (name == null) throw new ArgumentNullException(nameof(name));|}

        _name = name;
    }
}
", @"
#nullable enable
using System;

public class C
{
    private readonly string _name;

    public C(string name)
    {
        _name = name;
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task TheFixKeepsTheValueACoalesceGuarded() => Fix(@"
#nullable enable
using System;

public class C
{
    private readonly string _name;

    public C(string name)
    {
        _name = {|FDW063:name ?? throw new ArgumentNullException(nameof(name))|};
    }
}
", @"
#nullable enable
using System;

public class C
{
    private readonly string _name;

    public C(string name)
    {
        _name = name;
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task TheFixRemovesAThrowIfNullStatementBetweenOthers() => Fix(@"
#nullable enable
using System;

public class C
{
    public void Run(string name)
    {
        var count = 1;
        {|FDW063:ArgumentNullException.ThrowIfNull(name);|}
        Console.WriteLine(name + count);
    }
}
", @"
#nullable enable
using System;

public class C
{
    public void Run(string name)
    {
        var count = 1;
        Console.WriteLine(name + count);
    }
}
").RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public Task FixAllRemovesNeighbouringGuardsInOnePass() => Fix(@"
#nullable enable
using System;

public class C
{
    private readonly string _value;
    private readonly string _other;

    public C(string first, string second, string third)
    {
        {|FDW063:ArgumentNullException.ThrowIfNull(first);|}
        {|FDW063:if (second is null)
        {
            throw new ArgumentNullException(nameof(second));
        }|}

        _value = first + second;
        _other = {|FDW063:third ?? throw new ArgumentNullException(nameof(third))|};
    }
}
", @"
#nullable enable
using System;

public class C
{
    private readonly string _value;
    private readonly string _other;

    public C(string first, string second, string third)
    {
        _value = first + second;
        _other = third;
    }
}
").RunAsync(TestContext.Current.CancellationToken);
}
