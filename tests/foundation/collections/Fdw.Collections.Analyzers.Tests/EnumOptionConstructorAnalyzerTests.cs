using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="EnumOptionConstructorAnalyzer"/>.
/// </summary>
public class EnumOptionConstructorAnalyzerTests : AnalyzerTestBase<EnumOptionConstructorAnalyzer>
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
        public TestClass(string name) { }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumOptionWithPublicParameterlessConstructor_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public bool GenerateFactoryMethod { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption]
    public class TestOption
    {
        public TestOption() { }

        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumOptionWithGenerateFactoryMethodTrue_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public bool GenerateFactoryMethod { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(GenerateFactoryMethod = true)]
    public class TestOption
    {
        private TestOption() { }

        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ImplicitConstructor_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption]
    public class TestOption
    {
        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task RecordDeclaration_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption]
    public record TestOption
    {
        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticId_IsCorrect()
    {
        EnumOptionConstructorAnalyzer.DiagnosticId.ShouldBe("FDW036");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasOneRule()
    {
        var analyzer = new EnumOptionConstructorAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(1);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("FDW036");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SupportedDiagnosticDescriptor_HasCorrectProperties()
    {
        var analyzer = new EnumOptionConstructorAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("FDW036");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
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
    public class EnumOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption]
    public struct TestOption
    {
        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ProtectedConstructor_NoDiagnostics_WithFactoryMethod()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumOptionAttribute : Attribute
    {
        public bool GenerateFactoryMethod { get; set; }
    }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumOption(GenerateFactoryMethod = true)]
    public class TestOption
    {
        protected TestOption() { }

        public string Name => ""Test"";
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
