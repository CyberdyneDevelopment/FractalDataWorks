namespace Fdw.Services.Transformations.Abstractions;

/// <summary>
/// Canonical values for <see cref="TransformFieldDescriptor.InputKind"/>.
/// Consumers should compare with <c>StringComparison.Ordinal</c>.
/// </summary>
public static class TransformFieldKinds
{
    /// <summary>Single-line text input. Default for scalar values.</summary>
    public const string Text = "Text";

    /// <summary>Multi-line textarea for longer expressions or multi-entry blocks.</summary>
    public const string Textarea = "Textarea";
}
