namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Declarative descriptor for a single user-facing field on a command capability's configuration.
/// Lets the pipeline builder render the per-capability editor form without per-type hardcoded markup,
/// and lets persistence map each field key round-trip through the task Configuration dictionary.
/// </summary>
/// <param name="Key">
/// The wire key written into the task's Configuration dictionary when the value is persisted
/// (e.g., <c>"Query"</c>, <c>"TargetContainer"</c>, <c>"BatchSize"</c>).
/// </param>
/// <param name="Label">Human-facing label shown next to the input.</param>
/// <param name="Placeholder">Placeholder text shown inside an empty input.</param>
/// <param name="InputKind">
/// Which input shape the UI should render — one of <see cref="ConfigurationFieldKinds"/>
/// (<c>"Text"</c>, <c>"Textarea"</c>, <c>"Select"</c>, <c>"Int"</c>, <c>"Boolean"</c>,
/// <c>"KeyValueList"</c>). A string rather than an enum so consumers can add render kinds
/// from their own assemblies without a framework change.
/// Compare with <see cref="ConfigurationFieldKinds"/> constants using
/// <c>StringComparison.Ordinal</c>.
/// </param>
/// <param name="SelectOptions">
/// Options for <c>"Select"</c> kind fields. Each entry is <c>"Value:Label"</c> (colon-separated).
/// Ignored for other input kinds.
/// </param>
/// <param name="IsRequired">
/// Whether the field must have a non-empty value before the task can be saved.
/// Default <c>false</c>.
/// </param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record ConfigurationFieldDescriptor(
    string Key,
    string Label,
    string Placeholder,
    string InputKind,
    string[]? SelectOptions = null,
    bool IsRequired = false);
