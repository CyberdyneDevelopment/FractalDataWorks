namespace Fdw.Services.Catalog.Endpoints;

/// <summary>A glossary term as the list answers it: the term, and how many versions of it the caller may read (FDW-2548).</summary>
public class GlossaryTermListItem : GlossaryTermResponse
{
    /// <summary>
    /// Gets or sets how many versions of the term the caller may read, counting the term itself as one. It is 1 when no organization or workspace the
    /// caller may read has written its own text for it.
    /// </summary>
    public int VersionCount { get; set; }

    /// <summary>Writes a term as the list answers it.</summary>
    /// <param name="term">The term, as the other routes answer it.</param>
    /// <param name="versionCount">How many versions of it the caller may read.</param>
    /// <returns>The list item.</returns>
    internal static GlossaryTermListItem From(GlossaryTermResponse term, int versionCount) => new()
    {
        Id = term.Id,
        Name = term.Name,
        Definition = term.Definition,
        Category = term.Category,
        CategoryPath = term.CategoryPath,
        Owner = term.Owner,
        StandsFor = term.StandsFor,
        IsApproved = term.IsApproved,
        RelatedDataSets = term.RelatedDataSets,
        CreatedAt = term.CreatedAt,
        ModifiedAt = term.ModifiedAt,
        VersionCount = versionCount,
    };
}
