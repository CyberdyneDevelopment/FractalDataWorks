using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    Fdw.Conventions.Analyzers.LocalStatusClassMapperAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Fdw.Conventions.Analyzers.Tests;

/// <summary>
/// Tests for <see cref="LocalStatusClassMapperAnalyzer"/> (FDW048). The positive cases are the
/// literal shapes found in Fdw.UI.Pages; the negative cases are the near-misses the heuristic must
/// leave alone.
/// </summary>
public class LocalStatusClassMapperAnalyzerTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionOverStatusReturningBadgeClasses_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Operations/Pages/AuditPage.razor.
        var test = """
            namespace TestNamespace
            {
                public class AuditPage
                {
                    private static string {|#0:GetStateBadge|}(string state) => state switch
                    {
                        "Succeeded" => "badge b-ok",
                        "Completed" => "badge b-ok",
                        "Failed" => "badge b-fail",
                        "Running" => "badge b-run",
                        "Cancelled" => "badge b-warn",
                        _ => "badge b-idle"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetStateBadge"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task IfChainOverStatusReturningBadgeClasses_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Pipelines/.../PipelineExecutionPanel.razor.
        var test = """
            using System;

            namespace TestNamespace
            {
                public class PipelineExecutionPanel
                {
                    private static string {|#0:GetStatusBadgeClass|}(string status)
                    {
                        if (string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase))
                            return "b-ok";
                        if (string.Equals(status, "Paused", StringComparison.OrdinalIgnoreCase))
                            return "b-warn";
                        if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase))
                            return "b-fail";
                        return "b-idle";
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetStatusBadgeClass"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task ConditionalExpressionOverBoolReturningBadgeClasses_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Scheduling/.../SchedulesIndexPage.razor — the vocabulary
        // is carried by the method name alone, the parameter is a bool.
        var test = """
            namespace TestNamespace
            {
                public class SchedulesIndexPage
                {
                    private static string {|#0:GetStatusBadge|}(bool isActive) => isActive
                        ? "badge-success"
                        : "badge-idle";
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetStatusBadge"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionOverDomainTypeReturningBadgeClasses_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Connections/ConnectionList.razor — neither the parameter
        // name nor its type mentions status; the method name does.
        var test = """
            #nullable enable
            namespace TestNamespace
            {
                public class ConnectionPayload
                {
                    public bool? LastTestSuccess { get; set; }
                }

                public class ConnectionList
                {
                    private static string {|#0:GetHealthBadgeClass|}(ConnectionPayload conn) => conn.LastTestSuccess switch
                    {
                        true => "b-ok",
                        false => "b-fail",
                        null => "b-idle",
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetHealthBadgeClass"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionReturningCssVariables_ReportsDiagnosticFromParameterName()
    {
        // Shape copied from Fdw.UI.Pages/EtlProjects/.../ProjectExecutionPage.razor — "GetDotColor"
        // carries no status word, so the parameter name is what admits it.
        var test = """
            #nullable enable
            namespace TestNamespace
            {
                public class ProjectExecutionPage
                {
                    private static string {|#0:GetDotColor|}(string? status) => status switch
                    {
                        "Completed" => "var(--success)",
                        "Failed" => "var(--signal)",
                        _ => "var(--n-500)"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetDotColor"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task SwitchExpressionReturningHexColours_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Pipelines/.../PipelineBuilderPage.razor.
        var test = """
            #nullable enable
            namespace TestNamespace
            {
                public class PipelineBuilderPage
                {
                    private static string {|#0:GetTestStatusBorderColor|}(string? status) => status switch
                    {
                        "Running" => "#f59e0b",
                        "Complete" => "#22c55e",
                        "Failed" => "#ef4444",
                        _ => "#475569"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetTestStatusBorderColor"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task UnrelatedStringReturningSwitch_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public class GreetingCard
                {
                    private static string GetGreeting(string name) => name switch
                    {
                        "morning" => "Good morning",
                        "evening" => "Good evening",
                        _ => "Hello"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task StatusSwitchReturningDisplayText_NoDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Connections/ConnectionList.razor — GetHealthBadgeText maps
        // to a label, not to CSS, so the rule must leave it alone.
        var test = """
            #nullable enable
            namespace TestNamespace
            {
                public class ConnectionPayload
                {
                    public bool? LastTestSuccess { get; set; }
                }

                public class ConnectionList
                {
                    private static string GetHealthBadgeText(ConnectionPayload conn) => conn.LastTestSuccess switch
                    {
                        true => "Healthy",
                        false => "Unhealthy",
                        null => "Unknown",
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task StatusSwitchReturningKebabCaseData_NoDiagnostic()
    {
        // Kebab case alone is not a styling signal — a returned literal must carry a CSS class
        // marker, a hex colour, a custom property, or a declaration.
        var test = """
            namespace TestNamespace
            {
                public class WorkflowSummary
                {
                    private static string GetStateSlug(string state) => state switch
                    {
                        "Running" => "in-progress",
                        "Queued" => "not-started",
                        _ => "unknown-state"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task StatusSwitchMixingCssClassesAndProse_NoDiagnostic()
    {
        // Why: a helper that also returns prose is not purely a class mapper; reporting it would
        // suggest a migration that would lose information.
        var test = """
            namespace TestNamespace
            {
                public class MixedSummary
                {
                    private static string GetStatusSummary(string status) => status switch
                    {
                        "Completed" => "badge b-ok",
                        "Failed" => "The run did not complete",
                        _ => "badge b-idle"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task PublicStatusMapper_NoDiagnostic()
    {
        // A public helper is a deliberate shared API; FDW048 targets component-local forks.
        var test = """
            namespace TestNamespace
            {
                public class SharedBadges
                {
                    public static string GetStatusBadge(string status) => status switch
                    {
                        "Approved" => "badge b-ok",
                        "Rejected" => "badge b-fail",
                        _ => "badge b-idle"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task StatusMapperWithTwoParameters_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public class BadgeComposer
                {
                    private static string GetStatusBadge(string status, string size) => status switch
                    {
                        "Approved" => "badge b-ok",
                        "Rejected" => "badge b-fail",
                        _ => "badge b-idle"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task StatusSwitchReturningSingleDistinctClass_NoDiagnostic()
    {
        // One distinct class is a constant, not a mapping.
        var test = """
            namespace TestNamespace
            {
                public class ConstantBadge
                {
                    private static string GetStatusBadge(string status) => status switch
                    {
                        "Approved" => "badge b-ok",
                        _ => "badge b-ok"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Conventions")]
    public async Task NonStringStatusMapper_NoDiagnostic()
    {
        var test = """
            namespace TestNamespace
            {
                public class BadgeCount
                {
                    private static int GetStatusWeight(string status) => status switch
                    {
                        "Approved" => 1,
                        _ => 0
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task RoleToPillClasses_NoStatusVocabulary_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Authorization/Pages/UsersPage.razor (GetRolePillClass).
        // Nothing in the name, parameter or type says status/state/severity/health/badge -- this is
        // the case the earlier status-vocabulary gate missed for no principled reason.
        var test = """
            namespace TestNamespace
            {
                public class UsersPage
                {
                    private static string {|#0:GetRolePillClass|}(string role) => role switch
                    {
                        "Admin" => "bg-red-100 text-red-800 ring-red-200",
                        "Operator" => "bg-blue-100 text-blue-800 ring-blue-200",
                        _ => "bg-slate-100 text-slate-800 ring-slate-200"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetRolePillClass"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Conventions")]
    public async Task NodeTypeToHexColours_NoStatusVocabulary_ReportsDiagnostic()
    {
        // Shape copied from Fdw.UI.Pages/Calculations/Pages/CalculatedDesignerPage.razor
        // (GetNodeColor) -- structurally identical to GetTestStatusBorderColor, which the
        // vocabulary gate did catch. Both are component-local presentation mapping.
        var test = """
            namespace TestNamespace
            {
                public class CalculatedDesignerPage
                {
                    private static string {|#0:GetNodeColor|}(string nodeType) => nodeType switch
                    {
                        "Input" => "#4f8ef7",
                        "Operation" => "#e0a33a",
                        _ => "#8a8f98"
                    };
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(
            test,
            VerifyCS.Diagnostic("FDW048").WithLocation(0).WithArguments("GetNodeColor"));
    }
}
