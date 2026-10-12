using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="DuplicateLookupValueAnalyzer"/>.
/// </summary>
public class DuplicateLookupValueAnalyzerTests : AnalyzerTestBase<DuplicateLookupValueAnalyzer>
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EmptySource_NoDiagnostics()
    {
        var source = string.Empty;
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ClassWithoutEnumCollectionAttribute_NoDiagnostics()
    {
        var source = @"
namespace TestNamespace
{
    public class TestClass
    {
        public string Name { get; set; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollectionWithoutLookupProperties_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumCollection]
    public abstract class TestCollection
    {
        public abstract string Name { get; }
    }

    [EnumOption]
    public class Option1 : TestCollection
    {
        public override string Name => ""Option1"";
    }

    [EnumOption]
    public class Option2 : TestCollection
    {
        public override string Name => ""Option2"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollectionWithAllowMultipleTrue_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class EnumLookupAttribute : Attribute
    {
        public bool AllowMultiple { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumCollection]
    public abstract class TestCollection
    {
        public abstract string Name { get; }

        [EnumLookup(AllowMultiple = true)]
        public abstract string Code { get; }
    }

    [EnumOption]
    public class Option1 : TestCollection
    {
        public override string Name => ""Option1"";
        public override string Code => ""A"";
    }

    [EnumOption]
    public class Option2 : TestCollection
    {
        public override string Name => ""Option2"";
        public override string Code => ""A""; // Same code is OK with AllowMultiple
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticId_IsCorrect()
    {
        DuplicateLookupValueAnalyzer.DiagnosticId.ShouldBe("ENHENUM001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasOneRule()
    {
        var analyzer = new DuplicateLookupValueAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(1);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("ENHENUM001");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SupportedDiagnosticDescriptor_HasCorrectProperties()
    {
        var analyzer = new DuplicateLookupValueAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("ENHENUM001");
        descriptor.Category.ShouldBe("Collections");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }
}
