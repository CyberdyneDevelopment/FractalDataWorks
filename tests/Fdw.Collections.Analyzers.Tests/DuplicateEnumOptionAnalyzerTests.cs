using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="DuplicateEnumOptionAnalyzer"/>.
/// </summary>
public class DuplicateEnumOptionAnalyzerTests : AnalyzerTestBase<DuplicateEnumOptionAnalyzer>
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
    public async Task ClassWithoutEnumOptionAttribute_NoDiagnostics()
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
    public async Task UniqueEnumOptions_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public string Name { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option1"")]
    public class Option1
    {
    }

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option2"")]
    public class Option2
    {
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SameNameDifferentCollections_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public string Name { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(CollectionName = ""Collection1"", Name = ""Option"")]
    public class Option1
    {
    }

    [EnumOption(CollectionName = ""Collection2"", Name = ""Option"")]
    public class Option2
    {
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticId_IsCorrect()
    {
        DuplicateEnumOptionAnalyzer.DiagnosticId.ShouldBe("FDW035");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasOneRule()
    {
        var analyzer = new DuplicateEnumOptionAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(1);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("FDW035");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SupportedDiagnosticDescriptor_HasCorrectProperties()
    {
        var analyzer = new DuplicateEnumOptionAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("FDW035");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task RecordDeclaration_IsSupported()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public string Name { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option1"")]
    public record Option1;

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option2"")]
    public record Option2;
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task StructDeclaration_IsSupported()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class EnumOptionAttribute : Attribute
    {
        public string CollectionName { get; set; }
        public string Name { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option1"")]
    public struct Option1
    {
    }

    [EnumOption(CollectionName = ""TestCollection"", Name = ""Option2"")]
    public struct Option2
    {
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
