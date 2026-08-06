namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Canonical values for <see cref="ConfigurationFieldDescriptor.InputKind"/>.
/// Consumers should compare with <c>StringComparison.Ordinal</c>.
/// </summary>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class ConfigurationFieldKinds
{
    /// <summary>Single-line text input. Default for scalar string values.</summary>
    public const string Text = "Text";

    /// <summary>Multi-line textarea for longer expressions or multi-entry blocks.</summary>
    public const string Textarea = "Textarea";

    /// <summary>
    /// Dropdown select. Populate <see cref="ConfigurationFieldDescriptor.SelectOptions"/>
    /// with <c>"Value:Label"</c> entries.
    /// </summary>
    public const string Select = "Select";

    /// <summary>Numeric (integer) input. The stored value is a string representation of the number.</summary>
    public const string Numeric = "Int";

    /// <summary>Boolean toggle / checkbox.</summary>
    public const string Boolean = "Boolean";

    /// <summary>
    /// Key-value list editor. Stored as a JSON array of <c>{"Key":"…","Value":"…"}</c> objects
    /// in the Configuration dict.
    /// </summary>
    public const string KeyValueList = "KeyValueList";
}
