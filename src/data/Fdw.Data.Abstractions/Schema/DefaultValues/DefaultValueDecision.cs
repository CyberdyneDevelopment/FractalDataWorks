namespace Fdw.Data.Abstractions;

/// <summary>
/// What a <see cref="IDefaultValuePolicy"/> decided for one field on one write.
/// </summary>
/// <remarks>
/// Two states only: omit the column entirely (the store's own DEFAULT/IDENTITY/computed expression
/// fills it), or include it with a specific value. There is no third "invent a value" state — a
/// policy that cannot omit and has nothing to include is not this type's problem to solve; that is
/// a required-field failure the caller must raise, never something papered over here.
/// </remarks>
public readonly record struct DefaultValueDecision
{
    private DefaultValueDecision(bool shouldOmit, object? value)
    {
        ShouldOmit = shouldOmit;
        Value = value;
    }

    /// <summary>Gets whether the column should be left out of the write entirely.</summary>
    public bool ShouldOmit { get; }

    /// <summary>Gets the value to write. Meaningless when <see cref="ShouldOmit"/> is <see langword="true"/>.</summary>
    public object? Value { get; }

    /// <summary>The column is left out of the write; the store's own default fills it.</summary>
    public static DefaultValueDecision Omit() => new(true, null);

    /// <summary>The column is written with <paramref name="value"/>.</summary>
    public static DefaultValueDecision Include(object? value) => new(false, value);
}
