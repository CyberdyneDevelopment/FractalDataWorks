using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Primitives;

/// <summary>
/// The centred card body a page shows when a list has nothing in it, a record was not found, or a
/// load is still in flight.
/// </summary>
/// <remarks>
/// <para>
/// The block is a card body with generous padding and centred text — one shape, written out by hand
/// at forty-odd sites with the padding inline. The padding now comes from
/// <c>fdw-shapes.css</c> and the component names the shape.
/// </para>
/// <para>
/// <see cref="InCard"/> exists because roughly a third of the sites already sit inside a card that
/// carries a header, and the rest are the whole card. The parameter picks which, rather than the
/// component adding a wrapper element the site did not have.
/// </para>
/// </remarks>
public partial class EmptyState
{
    /// <summary>
    /// Gets or sets a value indicating whether the block is wrapped in its own <c>card</c>. Set it
    /// to <see langword="false"/> when the call site is already inside one.
    /// </summary>
    [Parameter]
    public bool InCard { get; set; } = true;

    /// <summary>
    /// Gets or sets a value selecting the square 48px padding instead of the default 48px/20px.
    /// Both spellings occur in the markup this replaces and they are not interchangeable — the side
    /// padding moves the wrap point of the message.
    /// </summary>
    [Parameter]
    public bool SquarePadding { get; set; }

    /// <summary>
    /// Gets or sets css classes appended to the body element, for the call sites that add a utility
    /// such as a muted colour or a monospace face.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the message shown in the block.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string BodyClass
    {
        get
        {
            var shape = SquarePadding ? "card-b fdw-empty-square" : "card-b fdw-empty";
            return string.IsNullOrEmpty(Class) ? shape : shape + " " + Class;
        }
    }
}
