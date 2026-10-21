using System.Collections.Generic;
using Fdw.Roslyn.Commands.Conventions.Results;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// The guide pages of the implementing document, with the commit they were verified against.
/// </summary>
internal sealed class ImplementationGuideCatalog
{
    /// <summary>Gets the commit the document was verified against.</summary>
    public required GuideProvenanceInfo Provenance { get; init; }

    /// <summary>Gets the pages that carry a guide object.</summary>
    public required IReadOnlyList<GuidePageModel> Pages { get; init; }
}
