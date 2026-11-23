using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Descriptions;

/// <summary>ServiceTypeCollection for the descriptions domain's service types.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(DescriptionServiceTypeBase),
    typeof(IDescriptionServiceType),
    typeof(DescriptionServiceTypes),
    ServiceCategory = "Descriptions",
    RestrictToCurrentCompilation = true)]
public partial class DescriptionServiceTypes : ServiceTypeCollectionBase<DescriptionServiceTypeBase, IDescriptionServiceType>
{
    /// <summary>
    /// The most characters of a text one row of <c>catalog.Description</c> holds: a longer description is stored as parts (FDW-6123), and a read joins them, so no caller
    /// sees one. A text within it is one row, Ordinal 1.
    /// </summary>
    /// <remarks>
    /// This is where the default is declared, and a host changes it at registration, before <c>Register</c> runs; a value below one is refused there. It is a tuning number: a
    /// larger one makes fewer rows for a long text, a smaller one keeps each row small. Nothing in code caps it, and the text column is <c>VARCHAR(MAX)</c>. A change applies to
    /// what is written from then on: a description already stored keeps the parts it has until it is saved again, and is read joined either way. The default is large enough
    /// that a normal description, a few paragraphs, is one part.
    /// </remarks>
    public static int PartLength { get; set; } = 4000;

    /// <summary>The connection this domain's configuration rows are read from and written to.</summary>
    public static string ConfigurationConnection => PlatformConnection.Name;
}
