using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Execution;

/// <summary>The wire shape of a <c>Decision</c>.</summary>
[ExcludeFromCodeCoverage]
internal sealed class DecisionDto
{
    public bool Permitted { get; set; }

    public string Reason { get; set; } = string.Empty;
}
