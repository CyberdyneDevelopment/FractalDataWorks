using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.ServiceServiceTypeCollectionAttributeAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class TypeCollectionAttributeAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithoutCollectionName_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute() { }
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                    public string? CollectionName { get; set; }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public class {|#0:ColorCollection|} : EnumOptionBase<ColorCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW030")
            .WithLocation(0)
            .WithArguments("ColorCollection");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithCollectionName_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute() { }
                    public ServiceServiceTypeCollectionAttribute(string collectionName)
                    {
                        CollectionName = collectionName;
                    }
                    public string? CollectionName { get; set; }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Colors")]
                public class ColorCollection : EnumOptionBase<ColorCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithNamedCollectionName_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection(CollectionName = "Statuses")]
                public class StatusCollection : EnumOptionBase<StatusCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_NotInheritingFromEnumOptionBase_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Colors")]
                public class {|#0:ColorCollection|}
                {
                    public string Name { get; } = string.Empty;
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW031")
            .WithLocation(0)
            .WithArguments("ColorCollection");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_InheritingFromServiceServiceTypeCollectionBase_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                }

                public abstract class ServiceServiceTypeCollectionBase<T> where T : ServiceServiceTypeCollectionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Colors")]
                public class ColorCollection : ServiceServiceTypeCollectionBase<ColorCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollectionAlias_WithCollectionName_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute
                {
                    public EnumCollectionAttribute(string collectionName) { }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection("Sizes")]
                public class SizeCollection : EnumOptionBase<SizeCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GenericEnumCollection_WithoutInterfaceConstraint_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                    public bool Generic { get; set; }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Items", Generic = true)]
                public class {|#0:ItemCollection|}<T> : EnumOptionBase<ItemCollection<T>>
                    where T : ItemCollection<T>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("FDW032")
            .WithLocation(0)
            .WithArguments("ItemCollection");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GenericEnumCollection_WithNonGenericInterfaceConstraint_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                    public bool Generic { get; set; }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }

                public interface IItem
                {
                    string Id { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Items", Generic = true)]
                public class ItemCollection<T> : EnumOptionBase<ItemCollection<T>>
                    where T : ItemCollection<T>, IItem
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonGenericEnumCollection_WithoutInterfaceConstraint_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Colors")]
                public class ColorCollection : EnumOptionBase<ColorCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleErrors_ReportsAllDiagnostics()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection]
                public class {|#0:ColorCollection|}
                {
                    public string Name { get; } = string.Empty;
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic("FDW030").WithLocation(0).WithArguments("ColorCollection"),
            VerifyCS.Diagnostic("FDW031").WithLocation(0).WithArguments("ColorCollection")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_InheritingThroughMultipleLevels_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceServiceTypeCollectionAttribute(string collectionName) { }
                }

                public abstract class EnumOptionBase<T> where T : EnumOptionBase<T>
                {
                    public abstract string Name { get; }
                }

                public abstract class IntermediateBase<T> : EnumOptionBase<T> where T : IntermediateBase<T>
                {
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [ServiceServiceTypeCollection("Colors")]
                public class ColorCollection : IntermediateBase<ColorCollection>
                {
                    public override string Name { get; } = string.Empty;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
