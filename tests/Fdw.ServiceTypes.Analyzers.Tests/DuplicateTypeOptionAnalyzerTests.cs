using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.DuplicateTypeOptionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

public class DuplicateTypeOptionAnalyzerTests
{

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DuplicateOptionNames_InSameCollection_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#0:RedColor|} : ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#1:AnotherRed|} : ColorBase { }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(0)
            .WithArguments("Red", "Colors");

        var expected2 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(1)
            .WithArguments("Red", "Colors");

        await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DuplicateOptionNames_WithEnumOptionAlias_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnumOptionBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class EnumOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnumOptionBase(CollectionName = "Statuses")]
                public abstract class StatusBase { }

                [EnumOption(Name = "Active", CollectionName = "Statuses")]
                public class {|#0:ActiveStatus|} : StatusBase { }

                [EnumOption(Name = "Active", CollectionName = "Statuses")]
                public class {|#1:AnotherActive|} : StatusBase { }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(0)
            .WithArguments("Active", "Statuses");

        var expected2 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(1)
            .WithArguments("Active", "Statuses");

        await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task SameOptionNames_InDifferentCollections_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                [EnhancedEnumBase(CollectionName = "Sizes")]
                public abstract class SizeBase { }

                [ServiceServiceTypeOption(Name = "Large", CollectionName = "Colors")]
                public class LargeColor : ColorBase { }

                [ServiceServiceTypeOption(Name = "Large", CollectionName = "Sizes")]
                public class LargeSize : SizeBase { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task UniqueOptionNames_InSameCollection_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class RedColor : ColorBase { }

                [ServiceServiceTypeOption(Name = "Blue", CollectionName = "Colors")]
                public class BlueColor : ColorBase { }

                [ServiceServiceTypeOption(Name = "Green", CollectionName = "Colors")]
                public class GreenColor : ColorBase { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task OptionWithoutAttribute_NoDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                public class RedColor : ColorBase { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task DuplicateOptionNames_DefaultNameFromTypeName_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Statuses")]
                public abstract class StatusBase { }

                [ServiceServiceTypeOption(CollectionName = "Statuses")]
                public class {|#0:Active|} : StatusBase { }

                [ServiceServiceTypeOption(Name = "Active", CollectionName = "Statuses")]
                public class {|#1:ActiveStatus|} : StatusBase { }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(0)
            .WithArguments("Active", "Statuses");

        var expected2 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(1)
            .WithArguments("Active", "Statuses");

        await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task CaseInsensitiveDuplicates_ReportsDiagnostic()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#0:RedColor|} : ColorBase { }

                [ServiceServiceTypeOption(Name = "RED", CollectionName = "Colors")]
                public class {|#1:RedUppercase|} : ColorBase { }
            }
            """;

        var expected1 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(0)
            .WithArguments("Red", "Colors");
        var expected2 = VerifyCS.Diagnostic("FDW026")
            .WithLocation(1)
            .WithArguments("RED", "Colors");

        await VerifyCS.VerifyAnalyzerAsync(test, expected1, expected2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task TripleDuplicateOptionNames_ReportsMultipleDiagnostics()
    {
        var test = """
            using System;

            namespace Fdw.ServiceTypes
            {
                [AttributeUsage(AttributeTargets.Class)]
                public class EnhancedEnumBaseAttribute : Attribute
                {
                    public string? CollectionName { get; set; }
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public class ServiceServiceTypeOptionAttribute : Attribute
                {
                    public string? Name { get; set; }
                    public string? CollectionName { get; set; }
                }
            }

            namespace TestNamespace
            {
                using Fdw.ServiceTypes;

                [EnhancedEnumBase(CollectionName = "Colors")]
                public abstract class ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#0:RedColor1|} : ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#1:RedColor2|} : ColorBase { }

                [ServiceServiceTypeOption(Name = "Red", CollectionName = "Colors")]
                public class {|#2:RedColor3|} : ColorBase { }
            }
            """;

        var expected = new[]
        {
            VerifyCS.Diagnostic("FDW026").WithLocation(0).WithArguments("Red", "Colors"),
            VerifyCS.Diagnostic("FDW026").WithLocation(1).WithArguments("Red", "Colors"),
            VerifyCS.Diagnostic("FDW026").WithLocation(2).WithArguments("Red", "Colors")
        };

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
