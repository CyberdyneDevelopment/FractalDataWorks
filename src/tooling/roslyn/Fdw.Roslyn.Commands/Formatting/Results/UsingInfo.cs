namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Information about a using directive.
/// </summary>
public sealed class UsingInfo
{
    /// <summary>
    /// Gets or sets the namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
}