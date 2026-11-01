using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.DuplicateLookupValueAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class DuplicateLookupValueAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithEnumLookupAttribute_NoDiagnosticWhenNotImplemented()
    {
        // Note: The analyzer currently has GetPropertyValue returning null,
        // so it won't actually detect duplicates. These tests verify the structure
        // and ensure no false positives are reported.
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Property)]
                public class EnumLookupAttribute : Attribute
                {
                    public bool AllowMultiple { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class StatusBase
                {
                    [EnumLookup]
                    public virtual int Code { get; protected set; }
                }

                [EnumOption]
                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus() { Code = 1; }
                }

                [EnumOption]
                public class InactiveStatus : StatusBase
                {
                    public InactiveStatus() { Code = 2; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithAllowMultipleTrue_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Property)]
                public class EnumLookupAttribute : Attribute
                {
                    public bool AllowMultiple { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class StatusBase
                {
                    [EnumLookup(AllowMultiple = true)]
                    public virtual int Category { get; protected set; }
                }

                [EnumOption]
                public class Status1 : StatusBase
                {
                    public Status1() { Category = 1; }
                }

                [EnumOption]
                public class Status2 : StatusBase
                {
                    public Status2() { Category = 1; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithoutEnumLookup_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class StatusBase
                {
                    public virtual int Code { get; protected set; }
                }

                [EnumOption]
                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus() { Code = 1; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task NonEnumCollection_WithEnumLookup_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Property)]
                public class EnumLookupAttribute : Attribute
                {
                    public bool AllowMultiple { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                public abstract class StatusBase
                {
                    [EnumLookup]
                    public virtual int Code { get; protected set; }
                }

                public class ActiveStatus : StatusBase
                {
                    public ActiveStatus() { Code = 1; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_MultipleEnumLookupProperties_WithAllowMultiple_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Property)]
                public class EnumLookupAttribute : Attribute
                {
                    public bool AllowMultiple { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class StatusBase
                {
                    [EnumLookup(AllowMultiple = true)]
                    public virtual int Code { get; protected set; }

                    [EnumLookup(AllowMultiple = true)]
                    public virtual string Category { get; protected set; } = string.Empty;
                }

                [EnumOption]
                public class Status1 : StatusBase
                {
                    public Status1()
                    {
                        Code = 1;
                        Category = "Active";
                    }
                }

                [EnumOption]
                public class Status2 : StatusBase
                {
                    public Status2()
                    {
                        Code = 1;
                        Category = "Active";
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task EnumCollection_WithConstructorParameter_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumCollectionAttribute : Attribute { }

                [AttributeUsage(AttributeTargets.Property)]
                public class EnumLookupAttribute : Attribute
                {
                    public EnumLookupAttribute() { }
                    public EnumLookupAttribute(bool allowMultiple)
                    {
                        AllowMultiple = allowMultiple;
                    }
                    public bool AllowMultiple { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionAttribute : Attribute { }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumCollection]
                public abstract class StatusBase
                {
                    [EnumLookup(allowMultiple: true)]
                    public virtual int Code { get; protected set; }
                }

                [EnumOption]
                public class Status1 : StatusBase
                {
                    public Status1() { Code = 1; }
                }

                [EnumOption]
                public class Status2 : StatusBase
                {
                    public Status2() { Code = 1; }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
