using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.MissingTypeOptionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class MissingTypeOptionAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeInheritingFromCollectionBase_WithoutTypeOption_ReportsWarning()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                public class {|#0:CreateCommand|} : CommandBase
                {
                    public override string Name { get; } = "Create";
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("TC001")
            .WithLocation(0)
            .WithArguments("CreateCommand", "CommandBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeInheritingFromCollectionBase_WithTypeOption_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                [ServiceTypeOption]
                public class CreateCommand : CommandBase
                {
                    public override string Name { get; } = "Create";
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task AbstractType_WithoutTypeOption_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                public abstract class QueryBase : CommandBase
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task InterfaceInheritingFromBase_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public interface ICommand
                {
                    string Name { get; }
                }

                [ServiceTypeCollection(typeof(ICommand))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                public interface ISpecificCommand : ICommand
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleTypesInheritingFromBase_SomeMissingTypeOption_ReportsWarnings()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                [ServiceTypeOption]
                public class CreateCommand : CommandBase
                {
                    public override string Name { get; } = "Create";
                }

                public class {|#0:UpdateCommand|} : CommandBase
                {
                    public override string Name { get; } = "Update";
                }

                public class {|#1:DeleteCommand|} : CommandBase
                {
                    public override string Name { get; } = "Delete";
                }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic("TC001").WithLocation(0).WithArguments("UpdateCommand", "CommandBase"),
            VerifyCS.Diagnostic("TC001").WithLocation(1).WithArguments("DeleteCommand", "CommandBase")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NestedTypeInheritingFromBase_WithoutTypeOption_ReportsWarning()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                public static class Commands
                {
                    public class {|#0:CreateCommand|} : CommandBase
                    {
                        public override string Name { get; } = "Create";
                    }
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("TC001")
            .WithLocation(0)
            .WithArguments("CreateCommand", "CommandBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeInheritingIndirectlyFromBase_WithoutTypeOption_ReportsWarning()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection
                {
                    public string Name { get; set; } = string.Empty;
                }

                public abstract class CrudCommandBase : CommandBase
                {
                }

                public class {|#0:CreateCommand|} : CrudCommandBase
                {
                    public override string Name { get; } = "Create";
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("TC001")
            .WithLocation(0)
            .WithArguments("CreateCommand", "CommandBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task MultipleCollections_DifferentBases_CorrectWarnings()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes.Attributes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeCollectionAttribute : Attribute
                {
                    public ServiceTypeCollectionAttribute(Type baseType) { }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class ServiceTypeOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using System;
                using Fdw.ServiceTypes.Attributes;

                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                public abstract class QueryBase
                {
                    public abstract string Name { get; }
                }

                [ServiceTypeCollection(typeof(CommandBase))]
                public class CommandCollection { }

                [ServiceTypeCollection(typeof(QueryBase))]
                public class QueryCollection { }

                public class {|#0:CreateCommand|} : CommandBase
                {
                    public override string Name { get; } = "Create";
                }

                [ServiceTypeOption]
                public class GetQuery : QueryBase
                {
                    public override string Name { get; } = "Get";
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("TC001")
            .WithLocation(0)
            .WithArguments("CreateCommand", "CommandBase");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NoTypeCollections_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                public class CreateCommand : CommandBase
                {
                    public override string Name { get; } = "Create";
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TypeCollectionAttributesNotAvailable_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public abstract class CommandBase
                {
                    public abstract string Name { get; }
                }

                public class CreateCommand : CommandBase
                {
                    public override string Name { get; } = "Create";
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
