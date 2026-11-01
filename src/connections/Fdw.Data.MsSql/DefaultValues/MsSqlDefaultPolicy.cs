using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// The framework-shipped default-value policy for MsSql connections.
/// </summary>
[TypeOption(typeof(MsSqlDefaults), "Default")]
public sealed class MsSqlDefaultPolicy : DefaultValuePolicyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlDefaultPolicy"/> class.
    /// </summary>
    public MsSqlDefaultPolicy()
        : base("Default")
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="IField.IsIdentity"/>, <see cref="IField.IsComputed"/> and
    /// <see cref="IField.IsSystemProvided"/> columns are never written, regardless of what the
    /// caller supplied — the store owns them entirely. A column with
    /// <see cref="IField.HasDefault"/> but no explicit value is omitted so the store's own DEFAULT
    /// fires; the same column with an explicit value is written as given, since HasDefault
    /// describes the column, not whether the app may override it.
    /// </remarks>
    public override DefaultValueDecision Value(IField field, bool hasExplicitValue, object? explicitValue)
    {
        if (field.IsIdentity || field.IsComputed || field.IsSystemProvided)
            return DefaultValueDecision.Omit();

        if (field.HasDefault && !hasExplicitValue)
            return DefaultValueDecision.Omit();

        return DefaultValueDecision.Include(explicitValue);
    }
}
