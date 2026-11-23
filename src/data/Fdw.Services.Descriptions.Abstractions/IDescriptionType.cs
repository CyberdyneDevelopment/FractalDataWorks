using Fdw.Collections;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A description type: a thing whose description is held in the descriptions table.</summary>
public interface IDescriptionType : ITypeOption<int, DescriptionTypeBase>
{
    /// <summary>
    /// Gets the permission that reads a thing of this type. A search shows a hit only to a caller who holds it, so the one table is never a way round
    /// the permission of the domain that owns the thing.
    /// </summary>
    string ReadPermission { get; }

    /// <summary>
    /// Gets whether a thing of this type belongs to an owner that names it, as a data set field belongs to its data set. A save of such a description states
    /// the owner's name, and a save of any other states none, so a hit names what it describes and where it is.
    /// </summary>
    bool HasOwner { get; }
}
