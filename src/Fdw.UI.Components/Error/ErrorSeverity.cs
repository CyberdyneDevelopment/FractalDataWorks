namespace Fdw.UI.Components.Error;

/// <summary>
/// Severity levels for error display rendering.
/// </summary>
// FDW017: TypeCollection would cause circular reference with source generators
#pragma warning disable FDW017
public enum ErrorSeverity
#pragma warning restore FDW017
{
    /// <summary>Informational — transient or expected issues (e.g., 503, 504).</summary>
    Info,

    /// <summary>Warning — permission or conflict issues (e.g., 403, 409).</summary>
    Warning,

    /// <summary>Error — server failures (e.g., 500-level).</summary>
    Error
}
