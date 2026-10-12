using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="MissingTypeOptionAnalyzer"/>.
/// </summary>
public class MissingTypeOptionAnalyzerTests : AnalyzerTestBase<MissingTypeOptionAnalyzer>
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
    public async Task ClassWithoutTypeCollectionAttribute_NoDiagnostics()
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
    public async Task TypeCollectionAttributeNotAvailable_NoDiagnostics()
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
    public async Task AbstractTypeInheritingFromBase_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypeCollectionAttribute : Attribute
    {
        public TypeCollectionAttribute(Type baseType, Type defaultReturnType) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    public abstract class BaseType { }

    [TypeCollection(typeof(BaseType), typeof(BaseType))]
    public class TestCollection { }

    public abstract class AbstractOption : BaseType { }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task InterfaceImplementation_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypeCollectionAttribute : Attribute
    {
        public TypeCollectionAttribute(Type baseType, Type defaultReturnType) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    public interface IBaseType { }

    [TypeCollection(typeof(IBaseType), typeof(IBaseType))]
    public class TestCollection { }

    public class ConcreteOption : IBaseType { }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticIds_AreCorrect()
    {
        MissingTypeOptionAnalyzer.MissingTypeOptionDiagnosticId.ShouldBe("TC001");
        MissingTypeOptionAnalyzer.GenericTypeMismatchDiagnosticId.ShouldBe("TC002");
        MissingTypeOptionAnalyzer.BaseTypeMismatchDiagnosticId.ShouldBe("TC003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasThreeRules()
    {
        var analyzer = new MissingTypeOptionAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(3);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("TC001");
        analyzer.SupportedDiagnostics[1].Id.ShouldBe("TC002");
        analyzer.SupportedDiagnostics[2].Id.ShouldBe("TC003");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MissingTypeOptionRule_HasCorrectProperties()
    {
        var analyzer = new MissingTypeOptionAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("TC001");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GenericMismatchRule_HasCorrectProperties()
    {
        var analyzer = new MissingTypeOptionAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[1];

        descriptor.Id.ShouldBe("TC002");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task BaseMismatchRule_HasCorrectProperties()
    {
        var analyzer = new MissingTypeOptionAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[2];

        descriptor.Id.ShouldBe("TC003");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NestedTypeCollection_IsSupported()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypeCollectionAttribute : Attribute
    {
        public TypeCollectionAttribute(Type baseType, Type defaultReturnType) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeOptionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    public class OuterClass
    {
        public abstract class BaseType { }

        [TypeCollection(typeof(BaseType), typeof(BaseType))]
        public class TestCollection { }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleNamespaces_IsSupported()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypeCollectionAttribute : Attribute
    {
        public TypeCollectionAttribute(Type baseType, Type defaultReturnType) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeOptionAttribute : Attribute { }
}

namespace TestNamespace.Models
{
    public abstract class BaseType { }
}

namespace TestNamespace.Collections
{
    using Fdw.Collections.Attributes;
    using TestNamespace.Models;

    [TypeCollection(typeof(BaseType), typeof(BaseType))]
    public class TestCollection { }
}";
        await VerifyNoDiagnostics(source);
    }
}
