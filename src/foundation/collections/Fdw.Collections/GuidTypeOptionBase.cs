using System;

namespace Fdw.Collections;

/// <summary>
/// Base class for type options keyed on <see cref="Guid"/>. The id is derived from the option's fully
/// qualified type name, the way <see cref="TypeOptionBase{TBase}"/> derives an int: an option never
/// supplies, overrides or hides its id.
/// </summary>
/// <typeparam name="TBase">The option family's base or interface (CRTP pattern).</typeparam>
public abstract class GuidTypeOptionBase<TBase> : TypeOptionBase<Guid, TBase>
    where TBase : ITypeOption<Guid, TBase>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuidTypeOptionBase{TBase}"/> class.
    /// </summary>
    /// <param name="name">The name of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected GuidTypeOptionBase(string name) : base(Guid.Empty, name)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuidTypeOptionBase{TBase}"/> class with display metadata.
    /// </summary>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="displayName">The display name for user-facing representations.</param>
    /// <param name="description">The detailed description of this type option.</param>
    /// <param name="category">The category of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected GuidTypeOptionBase(string name, string displayName, string description, string? category)
        : base(Guid.Empty, name, displayName, description, category)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GuidTypeOptionBase{TBase}"/> class with full metadata.
    /// </summary>
    /// <param name="name">The name of this type option value.</param>
    /// <param name="configurationKey">The configuration key for service registration and lookups.</param>
    /// <param name="displayName">The display name for user-facing representations.</param>
    /// <param name="description">The detailed description of this type option.</param>
    /// <param name="category">The category of this type option value.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    protected GuidTypeOptionBase(string name, string configurationKey, string displayName, string description, string? category)
        : base(Guid.Empty, name, configurationKey, displayName, description, category)
    {
    }

    private Guid? _derivedId;

    /// <inheritdoc />
    /// <remarks>
    /// <see cref="OptionId.Derive"/> of the option's fully qualified type name: the one Guid-for-a-name
    /// function, the same one service types and the generated collections use. Why an override and not
    /// the base initializer: the concrete type is not available there, and GetType() here returns the
    /// option itself. The Guid.Empty passed up is never read.
    /// </remarks>
    public override Guid Id => _derivedId ??= OptionId.Derive(GetType().FullName!);
}
