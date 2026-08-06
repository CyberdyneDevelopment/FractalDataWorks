using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit.v3;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="AbstractMemberAnalyzer"/>.
/// </summary>
public class AbstractMemberAnalyzerTests : AnalyzerTestBase<AbstractMemberAnalyzer>
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
    public abstract class TestClass
    {
        public abstract string Name { get; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task VirtualPropertyInEnumCollection_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumCollection]
    public abstract class TestCollection
    {
        public virtual string Code { get; protected set; }

        protected TestCollection(string code)
        {
            Code = code;
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task ConcretePropertyInEnumCollection_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumCollection]
    public abstract class TestCollection
    {
        public string Code { get; protected set; }

        protected TestCollection(string code)
        {
            Code = code;
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonAbstractClass_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [EnumCollection]
    public class TestCollection
    {
        public string Code { get; set; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticIds_AreCorrect()
    {
        AbstractMemberAnalyzer.AbstractPropertyDiagnosticId.ShouldBe("FDW037");
        AbstractMemberAnalyzer.AbstractFieldDiagnosticId.ShouldBe("FDW038");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasTwoRules()
    {
        var analyzer = new AbstractMemberAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(2);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("FDW037");
        analyzer.SupportedDiagnostics[1].Id.ShouldBe("FDW038");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractPropertyRule_HasCorrectProperties()
    {
        var analyzer = new AbstractMemberAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("FDW037");
        descriptor.Category.ShouldBe("Design");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractFieldRule_HasCorrectProperties()
    {
        var analyzer = new AbstractMemberAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[1];

        descriptor.Id.ShouldBe("FDW038");
        descriptor.Category.ShouldBe("Design");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NestedClass_IsSupported()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    public class OuterClass
    {
        [EnumCollection]
        public abstract class InnerCollection
        {
            public virtual string Code { get; protected set; }

            protected InnerCollection(string code)
            {
                Code = code;
            }
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
