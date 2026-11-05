using Fdw.Collections;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A kind of thing that can carry a tag.</summary>
public interface ITaggableKind : ITypeOption<int, TaggableKindBase>
{
    /// <summary>Gets the schema of the link table that holds the tags of this kind (the data path name in the configuration store).</summary>
    string PathName { get; }

    /// <summary>Gets the link table, which holds the thing's Id, the tag's Id and nothing else.</summary>
    string ContainerName { get; }

    /// <summary>Gets the column of the link table that holds the thing's Id.</summary>
    string ThingIdColumn { get; }

    /// <summary>
    /// Gets the name of this kind in <c>WorkspaceResourceKinds</c> when a thing of it sits in a workspace, so a tag owned by an organization or a
    /// workspace can be checked against the thing's place. An empty string means the kind is tenant-wide and takes tenant tags only.
    /// </summary>
    string WorkspaceResourceKind { get; }
}
