using Fdw.Collections;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>Base class for the kinds of thing that can carry a tag.</summary>
/// <remarks>
/// No id is passed: <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully qualified type name, so a package that owns a taggable
/// thing can declare its own kind. The kind's name is the part of this contract that must not change once links exist.
/// </remarks>
public abstract class TaggableKindBase : TypeOptionBase<TaggableKindBase>, ITaggableKind
{
    /// <summary>Initializes a new instance of the <see cref="TaggableKindBase"/> class.</summary>
    /// <param name="name">The kind name, which is what a request names.</param>
    /// <param name="pathName">The schema of the link table.</param>
    /// <param name="containerName">The link table.</param>
    /// <param name="thingIdColumn">The link table column that holds the thing's Id.</param>
    /// <param name="workspaceResourceKind">The workspace resource kind of the thing, or an empty string for a tenant-wide thing.</param>
    protected TaggableKindBase(string name, string pathName, string containerName, string thingIdColumn, string workspaceResourceKind)
        : base(name)
    {
        PathName = pathName;
        ContainerName = containerName;
        ThingIdColumn = thingIdColumn;
        WorkspaceResourceKind = workspaceResourceKind;
    }

    /// <inheritdoc />
    public string PathName { get; }

    /// <inheritdoc />
    public string ContainerName { get; }

    /// <inheritdoc />
    public string ThingIdColumn { get; }

    /// <inheritdoc />
    public string WorkspaceResourceKind { get; }
}
