using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// The card a page shows while its first load is in flight: one centred running badge and nothing else.
/// </summary>
/// <remarks>
/// A distinct component from <see cref="EmptyState"/> because it is a distinct shape — the block is a
/// flex row that centres a single pill, not a padded run of centred text — and because every site that
/// draws it draws exactly the same thing.
/// </remarks>
public partial class LoadingCard
{
    /// <summary>
    /// Gets or sets a value indicating whether the block is wrapped in its own <c>card</c>. Set it to
    /// <see langword="false"/> when the call site is already inside one.
    /// </summary>
    [Parameter]
    public bool InCard { get; set; } = true;

    /// <summary>
    /// Gets or sets the badge text.
    /// </summary>
    [Parameter]
    public string Text { get; set; } = "Loading";
}
