using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ErrorHandlingModeOptions;

/// <summary>
/// TypeCollection for error handling modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for error handling modes.
/// Source generator creates static properties for each registered error handling mode.
/// </remarks>
[TypeCollection(typeof(ErrorHandlingModeBase), typeof(IErrorHandlingMode), typeof(ErrorHandlingModes))]
public sealed partial class ErrorHandlingModes : TypeCollectionBase<ErrorHandlingModeBase, IErrorHandlingMode>
{
}
