using System.Threading.Tasks;
using Fdw.Collections.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Fdw.Collections.Analyzers.Tests;

/// <summary>
/// Tests for GenericTypeArgumentMismatchAnalyzer that validates generic type argument compatibility
/// between [TypeOption] attributes and base class inheritance.
/// </summary>
public sealed class GenericTypeArgumentMismatchAnalyzerTests : AnalyzerTestBase<GenericTypeArgumentMismatchAnalyzer>
{
    private const string AttributeDefinitions = @"
using System;

namespace Fdw.Collections.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeCollectionAttribute : Attribute
    {
        public TypeCollectionAttribute(Type baseType, Type defaultReturnType, Type collectionType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeOptionAttribute : Attribute
    {
        public TypeOptionAttribute(Type collectionType) { }
        public TypeOptionAttribute(Type collectionType, string name) { }
    }
}
";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoDiagnosticWhenGenericArgumentsMatch()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<string>))]
    public sealed class StringType : GenericBase<string>
    {
        public StringType() : base(1, ""StringType"") { }
    }
}";

        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DiagnosticWhenGenericArgumentsMismatch()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<string>))]
    public sealed class BadType : GenericBase<int>
    {
        public BadType() : base(1, ""BadType"") { }
    }
}";

        // AttributeDefinitions adds 17 lines, so [TypeOption] is on line 34 (17 + 17)
        // Use simpler diagnostic without exact span checking
        var expected = new DiagnosticResult(GenericTypeArgumentMismatchAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(34, 6);
        await VerifyDiagnostic(source, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoDiagnosticForNonGenericCollections()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(TestTypeBase), typeof(ITestType), typeof(TestTypes))]
    public partial class TestTypes { }

    public abstract class TestTypeBase
    {
        protected TestTypeBase(int id, string name) { }
    }

    public interface ITestType { }

    [TypeOption(typeof(TestTypes))]
    public sealed class TestType1 : TestTypeBase
    {
        public TestType1() : base(1, ""TestType1"") { }
    }
}";

        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoDiagnosticForOpenGenericInTypeOption()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<>))]
    public sealed class OpenGenericType : GenericBase<string>
    {
        public OpenGenericType() : base(1, ""OpenGenericType"") { }
    }
}";

        // Open generics in TypeOption are not validated by this analyzer
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DiagnosticForMultipleMismatchedTypes()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<string>))]
    public sealed class BadType1 : GenericBase<int>
    {
        public BadType1() : base(1, ""BadType1"") { }
    }

    [TypeOption(typeof(GenericTypes<int>))]
    public sealed class BadType2 : GenericBase<string>
    {
        public BadType2() : base(2, ""BadType2"") { }
    }
}";

        // AttributeDefinitions adds 17 lines, so [TypeOption] attributes are on lines 34 and 40
        var expected1 = new DiagnosticResult(GenericTypeArgumentMismatchAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(34, 6);
        var expected2 = new DiagnosticResult(GenericTypeArgumentMismatchAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(40, 6);

        await VerifyDiagnostics(source, expected1, expected2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoDiagnosticForAbstractTypes()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<string>))]
    public abstract class AbstractType : GenericBase<int>
    {
        protected AbstractType() : base(1, ""AbstractType"") { }
    }
}";

        // Abstract types are skipped by the analyzer
        await VerifyNoDiagnostics(source);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DiagnosticWithComplexGenericHierarchy()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    public abstract class IntermediateBase<T> : GenericBase<T>
    {
        protected IntermediateBase(int id, string name) : base(id, name) { }
    }

    [TypeOption(typeof(GenericTypes<string>))]
    public sealed class DerivedType : IntermediateBase<int>
    {
        public DerivedType() : base(1, ""DerivedType"") { }
    }
}";

        // AttributeDefinitions adds 17 lines, so [TypeOption] is on line 39 (17 + 22)
        var expected = new DiagnosticResult(GenericTypeArgumentMismatchAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(39, 6);
        await VerifyDiagnostic(source, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoDiagnosticWhenTypeParameterMatchesConstraint()
    {
        var source = AttributeDefinitions + @"
namespace TestNamespace
{
    using Fdw.Collections.Attributes;

    [TypeCollection(typeof(GenericBase<>), typeof(IGenericType), typeof(GenericTypes<>))]
    public partial class GenericTypes<T> { }

    public abstract class GenericBase<T>
    {
        protected GenericBase(int id, string name) { }
    }

    public interface IGenericType { }

    [TypeOption(typeof(GenericTypes<System.Guid>))]
    public sealed class GuidType : GenericBase<System.Guid>
    {
        public GuidType() : base(1, ""GuidType"") { }
    }
}";

        await VerifyNoDiagnostics(source);
    }
}
