namespace Fdw.Messages;

/// <summary>
/// Severity levels for framework messages.
/// </summary>
/// <remarks>
/// This enum provides enum-typed severity for use with <see cref="MessageTemplate{TSeverity}"/>.
/// The <see cref="MessageSeverities"/> TypeCollection provides the extensible type-safe alternative.
/// </remarks>
// FDW017: Intentionally kept as an enum. Fdw.Messages is a foundational package that
// predates and is referenced by the TypeCollection source generators themselves. Converting this to
// a TypeCollection would create a circular dependency between this package and the generator
// infrastructure. The MessageSeverities TypeCollection in Fdw.Messages serves as the
// extensible alternative for consumers above the core layer.
#pragma warning disable FDW017
public enum MessageSeverity
#pragma warning restore FDW017
{
    /// <summary>Debug-level messages for detailed diagnostic information.</summary>
    Debug = 0,

    /// <summary>Informational messages that provide context or status updates.</summary>
    Information = 1,

    /// <summary>Warning messages that indicate potential issues but don't prevent operation.</summary>
    Warning = 2,

    /// <summary>Error messages that indicate failures or critical problems.</summary>
    Error = 3,

    /// <summary>Critical messages that indicate system-level failures.</summary>
    Critical = 4
}
