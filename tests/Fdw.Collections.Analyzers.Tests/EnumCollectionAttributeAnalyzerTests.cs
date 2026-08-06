using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="EnumCollectionAttributeAnalyzer"/>.
/// </summary>
public class EnumCollectionAttributeAnalyzerTests : AnalyzerTestBase<EnumCollectionAttributeAnalyzer>
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
    public async Task EnumCollectionWithCollectionName_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute
    {
        public EnumCollectionAttribute(string collectionName) { }
    }
}

namespace Fdw.Collections
{
    public abstract class EnumOptionBase<T> { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;
    using Fdw.Collections;

    [EnumCollection(""TestCollection"")]
    public abstract class TestCollection : EnumOptionBase<TestCollection>
    {
        public abstract string Name { get; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollectionWithNamedParameter_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute
    {
        public string CollectionName { get; set; }
    }
}

namespace Fdw.Collections
{
    public abstract class EnumOptionBase<T> { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;
    using Fdw.Collections;

    [EnumCollection(CollectionName = ""TestCollection"")]
    public abstract class TestCollection : EnumOptionBase<TestCollection>
    {
        public abstract string Name { get; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollectionInheritingFromEnumCollectionBase_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute
    {
        public EnumCollectionAttribute(string collectionName) { }
    }
}

namespace Fdw.Collections
{
    public abstract class EnumCollectionBase<T> { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;
    using Fdw.Collections;

    [EnumCollection(""TestCollection"")]
    public abstract class TestCollection : EnumCollectionBase<TestCollection>
    {
        public abstract string Name { get; }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerDiagnosticIds_AreCorrect()
    {
        EnumCollectionAttributeAnalyzer.MissingCollectionNameDiagnosticId.ShouldBe("FDW039");
        EnumCollectionAttributeAnalyzer.MissingInheritanceDiagnosticId.ShouldBe("FDW040");
        EnumCollectionAttributeAnalyzer.GenericMustUseInterfaceDiagnosticId.ShouldBe("FDW041");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AnalyzerSupportedDiagnostics_HasThreeRules()
    {
        var analyzer = new EnumCollectionAttributeAnalyzer();
        analyzer.SupportedDiagnostics.Length.ShouldBe(3);
        analyzer.SupportedDiagnostics[0].Id.ShouldBe("FDW039");
        analyzer.SupportedDiagnostics[1].Id.ShouldBe("FDW040");
        analyzer.SupportedDiagnostics[2].Id.ShouldBe("FDW041");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MissingCollectionNameRule_HasCorrectProperties()
    {
        var analyzer = new EnumCollectionAttributeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[0];

        descriptor.Id.ShouldBe("FDW039");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MissingInheritanceRule_HasCorrectProperties()
    {
        var analyzer = new EnumCollectionAttributeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[1];

        descriptor.Id.ShouldBe("FDW040");
        descriptor.Category.ShouldBe("Usage");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GenericMustUseInterfaceRule_HasCorrectProperties()
    {
        var analyzer = new EnumCollectionAttributeAnalyzer();
        var descriptor = analyzer.SupportedDiagnostics[2];

        descriptor.Id.ShouldBe("FDW041");
        descriptor.Category.ShouldBe("Usage");
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
    public class EnumCollectionAttribute : Attribute
    {
        public EnumCollectionAttribute(string collectionName) { }
    }
}

namespace Fdw.Collections
{
    public abstract class EnumOptionBase<T> { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;
    using Fdw.Collections;

    public class OuterClass
    {
        [EnumCollection(""TestCollection"")]
        public abstract class TestCollection : EnumOptionBase<TestCollection>
        {
            public abstract string Name { get; }
        }
    }
}";
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonGenericEnumCollection_NoDiagnostics()
    {
        var source = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnumCollectionAttribute : Attribute
    {
        public EnumCollectionAttribute(string collectionName) { }
        public bool Generic { get; set; }
    }
}

namespace Fdw.Collections
{
    public abstract class EnumOptionBase<T> { }
}

namespace TestNamespace
{
    using Fdw.Collections.Attributes;
    using Fdw.Collections;

    [EnumCollection(""TestCollection"", Generic = false)]
    public abstract class TestCollection : EnumOptionBase<TestCollection>
    {
        public abstract string Name { get; }
    }
}";
        await VerifyNoDiagnostics(source);
    }
}
