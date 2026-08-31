using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.Note</c> — an observation someone leaves on a resource.
/// </summary>
/// <remarks>
/// <para>
/// A note is not an annotation. <c>catalog.DataSetAnnotation</c> is the durable description of a
/// data set — business owner, classification, update frequency — and stays where it is. A note is
/// what somebody thought while looking at something.
/// </para>
/// <para>
/// Why there is no title: a note is weightless — body plus subject, no kind, no assignee. The
/// moment it needs a field it has stopped being a note and should be promoted to a request, which
/// is where structure appears. This replaces the two unwired note tables rather than joining them.
/// </para>
/// <para>
/// Why <see cref="SubjectType"/> admits a snapshot: the place people most want to leave a note is
/// a value, and a cell reference is not stable — the same query answers differently tomorrow, so
/// a note pinned to one decays into a comment about nothing. Noting a value captures a snapshot
/// first and notes the capture, which carries its own verdict and trace hash.
/// </para>
/// </remarks>
[GenerateMapper]
public sealed partial class NoteConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "Notes";

    /// <summary>Gets the structural discriminator.</summary>
    public string ServiceType => "Note";

    /// <summary>Gets the service option type. Always null — this row selects no factory.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the kind of thing this note is about.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject's logical identity.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the universe the note was raised in, when there is one. A note may be left on
    /// a shared resource outside any project.
    /// </summary>
    public Guid? UniverseId { get; set; }

    /// <summary>Gets or sets the note text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets who wrote it.</summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the request this note became, once someone promoted it.</summary>
    public Guid? PromotedToRequestId { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
