using System;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of pagination state.
/// </summary>
public sealed class PaginationState : IPaginationState
{
    /// <inheritdoc />
    public int CurrentPage { get; set; } = 1;

    /// <inheritdoc />
    public int PageSize { get; set; } = 20;

    /// <inheritdoc />
    public int TotalItems { get; set; }

    /// <inheritdoc />
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    /// <inheritdoc />
    public bool HasPreviousPage => CurrentPage > 1;

    /// <inheritdoc />
    public bool HasNextPage => CurrentPage < TotalPages;
}