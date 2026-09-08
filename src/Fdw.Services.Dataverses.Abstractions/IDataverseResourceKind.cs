using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>
/// A kind of resource that can be attached to a dataverse.
/// </summary>
public interface IDataverseResourceKind : ITypeOption<int, DataverseResourceKindBase>
{
    /// <summary>
    /// Gets whether a dataverse can own a resource of this kind, as opposed to only using it.
    /// </summary>
    /// <remarks>
    /// A dataverse owns the data sets it sketched, so archiving the project takes them with it. It
    /// does not own the shared connection it reads through — that belongs to the platform and
    /// serves every other project too. Ownership is therefore a property of the kind, not a free
    /// choice at attach time, and attaching with the wrong relationship is a structured failure.
    /// </remarks>
    bool CanBeOwned { get; }
}
