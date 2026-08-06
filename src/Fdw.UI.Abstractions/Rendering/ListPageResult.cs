using PageAction = Fdw.UI.Abstractions.Pages.IPageAction;

namespace Fdw.UI.Abstractions.Rendering;

/// <summary>
/// Result of rendering an <see cref="Pages.IListPageModel"/> through an <see cref="IUIRenderer"/>.
/// </summary>
/// <remarks>
/// <para>
/// Carries no rendering-framework types, so every renderer backend returns the same shape — the
/// caller reacts to the chosen <see cref="Action"/> without knowing which renderer painted the page.
/// Mirrors <see cref="PageResult"/>: get-only state plus intent-named static factories.
/// </para>
/// <para>
/// <see cref="ShouldExit"/> distinguishes "the caller should leave this page" (an action was chosen,
/// or the user went back) from "stay and re-render" (search or pagination changed the model in place).
/// </para>
/// <para>
/// Why the <c>PageAction</c> alias: this namespace declares its own <c>IPageAction</c> (used by
/// <see cref="PageResult"/>, keyed by int), which would shadow a plain using-import of the Pages one.
/// A list page's actions come from <c>IListPageModel.ListActions</c>, which are the Pages variant, so
/// the alias binds it unambiguously.
/// </para>
/// </remarks>
public sealed class ListPageResult
{
    private ListPageResult(
        bool success,
        bool shouldExit,
        PageAction? action,
        object? selectedRowId,
        int? selectedRowIndex,
        string? error)
    {
        Success = success;
        ShouldExit = shouldExit;
        Action = action;
        SelectedRowId = selectedRowId;
        SelectedRowIndex = selectedRowIndex;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the page rendered successfully.</summary>
    public bool Success { get; }

    /// <summary>Gets a value indicating whether the caller should leave the page.</summary>
    public bool ShouldExit { get; }

    /// <summary>Gets the action the user chose, if any.</summary>
    public PageAction? Action { get; }

    /// <summary>Gets the identifier of the row the action applies to, if a row was selected.</summary>
    public object? SelectedRowId { get; }

    /// <summary>Gets the index of the row the action applies to, if a row was selected.</summary>
    public int? SelectedRowIndex { get; }

    /// <summary>Gets the error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; }

    /// <summary>The page changed in place (search, or an invalid choice) — re-render and stay.</summary>
    public static ListPageResult Continue() =>
        new(success: true, shouldExit: false, action: null, selectedRowId: null, selectedRowIndex: null, error: null);

    /// <summary>An action that does not leave the page was chosen (e.g. pagination) — re-render and stay.</summary>
    public static ListPageResult Continue(PageAction action) =>
        new(success: true, shouldExit: false, action: action, selectedRowId: null, selectedRowIndex: null, error: null);

    /// <summary>The user chose an action — leave the page and let the caller handle it.</summary>
    public static ListPageResult Selected(PageAction action, object? selectedRowId = null, int? selectedRowIndex = null) =>
        new(success: true, shouldExit: true, action: action, selectedRowId: selectedRowId, selectedRowIndex: selectedRowIndex, error: null);

    /// <summary>The user went back without choosing an action.</summary>
    public static ListPageResult Exit() =>
        new(success: true, shouldExit: true, action: null, selectedRowId: null, selectedRowIndex: null, error: null);

    /// <summary>Rendering was cancelled.</summary>
    public static ListPageResult Cancel() =>
        new(success: true, shouldExit: true, action: null, selectedRowId: null, selectedRowIndex: null, error: null);

    /// <summary>Rendering failed — fail loud, never a silently empty page.</summary>
    public static ListPageResult Failure(string error) =>
        new(success: false, shouldExit: true, action: null, selectedRowId: null, selectedRowIndex: null, error: error);
}
