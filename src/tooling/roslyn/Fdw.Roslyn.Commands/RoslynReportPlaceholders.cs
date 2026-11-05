using Fdw.Conventions;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// The texts a Roslyn command's report shows where a value is absent: a filter the caller left out, which means every
/// project, and a name the solution does not carry. None of them is ever a real project, type or member name.
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, so the report texts are named
/// once, with the exact wording the reports have always used (FDW-3580).
/// </remarks>
[PlaceholderText]
public static class RoslynReportPlaceholders
{
    /// <summary>No project or collection filter: every one is in scope.</summary>
    public const string All = "(all)";

    /// <summary>No scope: the whole solution is searched.</summary>
    public const string WholeSolution = "(whole solution)";

    /// <summary>A value Roslyn does not report, such as a non-C# project's language version.</summary>
    public const string Unknown = "Unknown";

    /// <summary>A name the syntax does not carry, shown in parentheses.</summary>
    public const string UnknownInParentheses = "(unknown)";

    /// <summary>A name the syntax does not carry, in lower case.</summary>
    public const string UnknownLowerCase = "unknown";

    /// <summary>An assembly the compilation does not name.</summary>
    public const string UnknownAssembly = "<unknown>";

    /// <summary>A move run without the reason the caller may give.</summary>
    public const string NoReasonGiven = "(no reason given)";

    /// <summary>The C# text of a null constant.</summary>
    public const string NullConstant = "null";
}
