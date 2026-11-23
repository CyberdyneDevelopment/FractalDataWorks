using Fdw.Collections;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>Base class for the description types.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name, so the package that owns a described thing
/// declares its own type without touching this one. The type's name is the part of this contract that must not change once descriptions exist.
/// </remarks>
public abstract class DescriptionTypeBase : TypeOptionBase<DescriptionTypeBase>, IDescriptionType
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionTypeBase"/> class.</summary>
    /// <param name="name">The type name, which is what a request and the table's <c>DescriptionType</c> column name.</param>
    /// <param name="readPermission">The permission that reads a thing of this type.</param>
    /// <param name="hasOwner">Whether a thing of this type belongs to an owner that names it.</param>
    protected DescriptionTypeBase(string name, string readPermission, bool hasOwner)
        : base(name)
    {
        // No throw here: a type option is built by the module initializer before Main, where a throw would end the host before any log or coded message (FDW-2931).
        // The Descriptions service type checks every option's read permission when the host initializes (DescriptionTypeRequirements) and stops it with a coded message.
        ReadPermission = readPermission;
        HasOwner = hasOwner;
    }

    /// <inheritdoc />
    public string ReadPermission { get; }

    /// <inheritdoc />
    public bool HasOwner { get; }
}
