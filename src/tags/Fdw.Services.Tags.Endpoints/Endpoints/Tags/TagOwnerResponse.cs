using System;

namespace Fdw.Services.Tags.Endpoints;

/// <summary>One owner a caller may give a tag to: the kind (one of <c>TagOwnerKinds</c>), the owner's Id and its name.</summary>
public sealed class TagOwnerResponse
{
    /// <summary>Gets or sets the owner kind, exactly as <c>OwnerKind</c> on a tag takes it.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner's Id, the <c>OwnerId</c> a tag takes.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owner's name as a person reads it.</summary>
    public string Name { get; set; } = string.Empty;
}
