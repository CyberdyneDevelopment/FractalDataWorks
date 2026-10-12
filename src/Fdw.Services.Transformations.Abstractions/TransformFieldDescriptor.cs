namespace Fdw.Services.Transformations.Abstractions;

/// <summary>
/// Declarative descriptor for a single user-facing field on a transformation's configuration.
/// Lets UI builders render the per-type editor form without per-type hardcoded markup, and
/// lets persistence map each field key round-trip to the matching sub-type column.
/// </summary>
/// <param name="Key">The wire/config-dictionary key used when serializing into the pipeline task configuration.</param>
/// <param name="Label">Human-facing label shown next to the input.</param>
/// <param name="Placeholder">Placeholder text shown inside an empty input.</param>
/// <param name="InputKind">
/// Which input shape the UI should render — one of <see cref="TransformFieldKinds"/>
/// (<c>"Text"</c> for single-line, <c>"Textarea"</c> for multi-line). A string rather than
/// an enum so the surface stays open to additional render kinds without an FDW017 violation.
/// </param>
public sealed record TransformFieldDescriptor(
    string Key,
    string Label,
    string Placeholder,
    string InputKind);
