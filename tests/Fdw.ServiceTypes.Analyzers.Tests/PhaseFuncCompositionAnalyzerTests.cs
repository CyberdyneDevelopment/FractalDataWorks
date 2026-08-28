using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.ServiceTypes.Analyzers.PhaseFuncCompositionAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.ServiceTypes.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="PhaseFuncCompositionAnalyzer"/> — STC001, where the line falls between a class
/// setting the phase func it owns and one composing onto a body somebody else holds; and STC002, which
/// keeps everything between ServiceTypeBase and the declared service type out of the phase entirely.
/// </summary>
public class PhaseFuncCompositionAnalyzerTests
{
    private const string Fixture = """
        using System;

        namespace Fdw.Collections.Attributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            public class ServiceTypeOptionAttribute : Attribute
            {
                public ServiceTypeOptionAttribute(Type collectionType, string name) { }
            }

            [AttributeUsage(AttributeTargets.Class)]
            public class ServiceTypeCollectionAttribute : Attribute
            {
            }
        }

        namespace Fdw.Services.Abstractions
        {
            public abstract class ServiceTypeBase
            {
                public void Configuration(Func<int, int> method) { }

                public void Registration(Func<int, int> method) { }

                public void Initialization(Func<int, int> method) { }

                public void AppendConfiguration(Func<int, int> method) { }

                public void PrependConfiguration(Func<int, int> method) { }

                public void AppendRegistration(Func<int, int> method) { }

                public void PrependRegistration(Func<int, int> method) { }

                public void AppendInitialization(Func<int, int> method) { }

                public void PrependInitialization(Func<int, int> method) { }
            }
        }

        """;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AppendRegistration_InServiceTypeOption_ReportsDiagnostic()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypes { }

                [ServiceTypeOption(typeof(ConnectionTypes), "MsSql")]
                public sealed class MsSqlConnectionType : ServiceTypeBase
                {
                    public MsSqlConnectionType()
                    {
                        {|#0:AppendRegistration|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC001").WithLocation(0).WithArguments("AppendRegistration", "Registration"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task PrependRegistration_InServiceTypeOption_ReportsDiagnostic()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypes { }

                [ServiceTypeOption(typeof(ConnectionTypes), "MsSql")]
                public sealed class MsSqlConnectionType : ServiceTypeBase
                {
                    public MsSqlConnectionType()
                    {
                        {|#0:PrependRegistration|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC001").WithLocation(0).WithArguments("PrependRegistration", "Registration"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AppendConfigurationAndInitialization_NameTheirOwnSetters()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypes { }

                [ServiceTypeOption(typeof(ConnectionTypes), "MsSql")]
                public sealed class MsSqlConnectionType : ServiceTypeBase
                {
                    public MsSqlConnectionType()
                    {
                        {|#0:AppendConfiguration|}(value => value);
                        {|#1:PrependInitialization|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC001").WithLocation(0).WithArguments("AppendConfiguration", "Configuration"),
            VerifyCS.Diagnostic("STC001").WithLocation(1).WithArguments("PrependInitialization", "Initialization"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AppendRegistration_InServiceTypeCollection_ReportsDiagnostic()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;
                using Fdw.Services.Abstractions;

                [ServiceTypeCollection]
                public sealed class ConnectionTypes : ServiceTypeBase
                {
                    public ConnectionTypes()
                    {
                        {|#0:AppendRegistration|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC001").WithLocation(0).WithArguments("AppendRegistration", "Registration"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task Registration_InServiceTypeOption_ReportsNothing()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Collections.Attributes;
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypes { }

                [ServiceTypeOption(typeof(ConnectionTypes), "MsSql")]
                public sealed class MsSqlConnectionType : ServiceTypeBase
                {
                    public MsSqlConnectionType()
                    {
                        Registration(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task AppendRegistration_FromAnUnattributedConsumer_ReportsNothing()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public sealed class SomeoneElsesOption : ServiceTypeBase
                {
                }

                public static class HostWiring
                {
                    public static void Customise(SomeoneElsesOption option)
                    {
                        option.AppendRegistration(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Analyzer")]
    public async Task Registration_FromAnIntermediateBaseClass_ReportsDiagnostic()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypeBase : ServiceTypeBase
                {
                    protected ConnectionTypeBase()
                    {
                        {|#0:Registration|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC002").WithLocation(0).WithArguments("Registration"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task PrependRegistration_FromAnIntermediateBaseClass_ReportsComposition()
    {
        var test = Fixture + """
            namespace TestNamespace
            {
                using Fdw.Services.Abstractions;

                public abstract class ConnectionTypeBase : ServiceTypeBase
                {
                    protected ConnectionTypeBase()
                    {
                        {|#0:PrependRegistration|}(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("STC001").WithLocation(0).WithArguments("PrependRegistration", "Registration"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Analyzer")]
    public async Task AppendRegistration_OnATypeOptionBase_ReportsNothing()
    {
        var test = """
            using System;

            namespace Fdw.Collections
            {
                public abstract class TypeOptionBase
                {
                    public void AppendRegistration(Func<int, int> method) { }
                }
            }

            namespace TestNamespace
            {
                using Fdw.Collections;

                public sealed class ActiveStatus : TypeOptionBase
                {
                    public ActiveStatus()
                    {
                        AppendRegistration(value => value);
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }
}
