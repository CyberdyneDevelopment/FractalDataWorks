namespace Fdw.Data.Abstractions;

/// <summary>
/// Extension methods for <see cref="IDataField"/> that provide derived state checks
/// without requiring default interface implementations (which are unsupported on
/// <c>netstandard2.0</c> targets).
/// </summary>
public static class DataFieldExtensions
{
    /// <summary>
    /// Gets the effective type of this field: <see cref="IFieldBinding.ResultType"/> when
    /// bound, otherwise <see cref="IDataField.ExplicitType"/>.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns>The resolved <see cref="IDataType"/>, or <see langword="null"/> when neither
    /// binding nor explicit type is set.</returns>
    public static IDataType? ResolvedType(this IDataField field)
        => field.IsBound() ? field.Binding!.ResultType : field.ExplicitType;

    /// <summary>
    /// Returns <see langword="true"/> when the field has a non-empty <see cref="IDataNode.Name"/>.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    public static bool IsDescribed(this IDataField field)
        => !string.IsNullOrEmpty(field.Name);

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="IDataField.ExplicitType"/> has been declared.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    public static bool IsDefined(this IDataField field)
        => field.ExplicitType is not null;

    /// <summary>
    /// Returns <see langword="true"/> when <see cref="IDataField.Binding"/> is present and the
    /// field can participate in query generation.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    public static bool IsBound(this IDataField field)
        => field.Binding is not null;
}
