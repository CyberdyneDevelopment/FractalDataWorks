using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Whether a container field may be projected into a dataset.
/// </summary>
/// <remarks>
/// Why a TypeCollection and not a bool: "visible" and "not visible" are the two answers needed
/// today, and masking — returning the field with its value obscured rather than omitting it — is a
/// third answer to the same question. A bool cannot grow that third case without every caller
/// learning a second flag.
/// </remarks>
public interface IFieldVisibility : ITypeOption<int, FieldVisibilityBase>
{
    /// <summary>Gets a value indicating whether the field may appear in a dataset projection.</summary>
    bool AllowsProjection { get; }
}
