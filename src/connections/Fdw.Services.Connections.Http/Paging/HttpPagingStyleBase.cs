using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Http.Paging;

/// <summary>
/// A way a remote HTTP API pages its rows (FDW-4938). It declares the additional properties it reads, refuses a connection that leaves out
/// one it needs, and builds the plan one read follows through the remote's pages.
/// </summary>
/// <remarks>
/// The tuning values (<see cref="HttpPagingProperties.PageSize"/>, <see cref="HttpPagingProperties.MaxPages"/>) are declared here, once for
/// every style, as the defaults a connection takes when it states none (<see cref="IDeclaresDefaults"/>, Mike: "the thing you select carries
/// it"). A host changes one from a Configure body with <c>ServiceTypeDefaults.Override(HttpPagingStyles.OffsetLimit, "PagingPageSize", 500)</c>
/// for one style or <c>ServiceTypeDefaults.OverrideForCategory("Connection", "PagingPageSize", 500)</c> for every style, and a value the
/// connection states wins over both. What only the connection can know (parameter names, the first page's number, a path in the body) has
/// no default: the style refuses a connection that leaves it out.
/// </remarks>
public abstract class HttpPagingStyleBase : TypeOptionBase<HttpPagingStyleBase>, IDeclaresDefaults
{
    // The tuning values: the properties that mean the same in every style, so a container takes them from a connection that pages in another style.
    private static readonly string[] Tuning = [HttpPagingProperties.PageSize, HttpPagingProperties.MaxPages];

    private static readonly IReadOnlyDictionary<string, object> Shared = ServiceTypeDefaults.With(
        new Dictionary<string, object>(StringComparer.Ordinal),
        (HttpPagingProperties.PageSize, 100),
        (HttpPagingProperties.MaxPages, 100000));

    /// <summary>Initializes the Empty/NotFound sentinel (source-generated).</summary>
    protected HttpPagingStyleBase()
        : base(string.Empty)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="HttpPagingStyleBase"/> class.</summary>
    /// <param name="name">The style's name, as a connection's PagingStyle states it.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">What the style does.</param>
    /// <param name="expectedProperties">Every additional property the style reads.</param>
    /// <param name="requiredProperties">The additional properties a connection must state for the style.</param>
    protected HttpPagingStyleBase(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> expectedProperties,
        IReadOnlyList<string> requiredProperties)
        : base(name, $"HttpPaging:{name}", displayName, description, "Connection")
    {
        ExpectedProperties = expectedProperties;
        RequiredProperties = requiredProperties;
        InheritableProperties = [.. expectedProperties.Where(property => Tuning.Contains(property, StringComparer.OrdinalIgnoreCase))];
    }

    /// <summary>Gets every additional property the style reads; the endpoint shows these and accepts them beside the authentication keys.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; } = [];

    /// <summary>Gets the additional properties a connection must state for the style.</summary>
    public IReadOnlyList<string> RequiredProperties { get; } = [];

    /// <summary>
    /// Gets the properties of this style a container that pages in this style takes from its connection even when the connection pages in another style
    /// (FDW-5562): the tuning values, which mean the same in every style. Every other property is the container's to state; it is never read off a
    /// connection that states it for a style of its own. When the container and the connection page in the same style, every property is taken from the
    /// connection that the container does not state.
    /// </summary>
    public IReadOnlyList<string> InheritableProperties { get; } = [];

    /// <summary>Gets whether this is the Empty/NotFound sentinel.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>Gets whether the style reads more than one page, writing its position into the request's query string.</summary>
    public virtual bool Pages => true;

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> DeclaredDefaults => Shared;

    /// <summary>Builds one read's plan from the connection's additional properties, refusing one that leaves out what the style needs.</summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="window">The rows the read returns: <see cref="HttpReadWindow.All"/>, or the window a query asks for.</param>
    /// <returns>The plan, or the refusal naming the property that is missing or wrong, or the window the style cannot address.</returns>
    public IGenericResult<HttpPagingPlan> Plan(HttpPagingSettings settings, HttpReadWindow window)
    {
        var stated = RequiredStated(settings);
        if (stated.IsFailure) return stated.ToNewResult<HttpPagingPlan>();

        // A read of no rows (Take 0) asks for nothing and returns nothing; only a negative window is refused.
        var counted = Counted(settings, window.Skip, window.Take, minimumTake: 0);
        return counted.IsFailure ? counted.ToNewResult<HttpPagingPlan>() : CreatePlan(settings, window);
    }

    /// <summary>
    /// Checks paging settings once, as the same code every place that stores or reads them does (FDW-5562): every property the style needs is stated, and
    /// every number it reads is a whole number in its range, or the style declares a default for it. A connection or a container that states a bad value is
    /// refused when it is saved, not at the first read.
    /// </summary>
    /// <param name="settings">The effective paging settings: the connection's, overlaid by the container's.</param>
    /// <returns>Success, or the refusal naming the property that is missing or wrong.</returns>
    public IGenericResult Check(HttpPagingSettings settings)
    {
        var stated = RequiredStated(settings);
        if (stated.IsFailure) return stated;

        foreach (var property in ExpectedProperties)
        {
            var minimum = NumberMinimum(property);
            if (minimum is null) continue;

            var number = WholeNumber(settings, property, minimum.Value);
            if (number.IsFailure) return number;
        }

        return GenericResult.Success();
    }

    /// <summary>Gets the keys, among those given, that this style does not read.</summary>
    /// <param name="keys">The setting names a connection or a container states.</param>
    /// <returns>The keys the style does not read, in the order given.</returns>
    public IReadOnlyList<string> UnexpectedProperties(IEnumerable<string> keys)
        => [.. keys.Where(key => !ExpectedProperties.Contains(key, StringComparer.OrdinalIgnoreCase))];

    // The smallest value of a numeric property, or null for one that is not a number.
    private static int? NumberMinimum(string property)
    {
        if (string.Equals(property, HttpPagingProperties.FirstPageNumber, StringComparison.OrdinalIgnoreCase)) return 0;
        return Tuning.Contains(property, StringComparer.OrdinalIgnoreCase) ? 1 : null;
    }

    /// <summary>
    /// Writes the window of rows a query asks for into one request (a query with paging, read as one response): the parameters, by the names
    /// the connection states, and what the reader cuts itself. A window the style cannot ask its remote for is refused, never approximated.
    /// </summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <param name="take">The most rows the query returns; null for all.</param>
    /// <returns>How the request asks for the window, or the refusal.</returns>
    public IGenericResult<HttpPagingWindow> Window(HttpPagingSettings settings, int skip, int? take)
    {
        var stated = RequiredStated(settings);
        if (stated.IsFailure) return stated.ToNewResult<HttpPagingWindow>();

        // One request asks the remote for its rows, and a limit of 0 is read by some remotes as "your default page", so it is refused.
        var counted = Counted(settings, skip, take, minimumTake: 1);
        return counted.IsFailure ? counted.ToNewResult<HttpPagingWindow>() : CreateWindow(settings, skip, take);
    }

    /// <summary>The style's own step: builds the plan once the required properties are known to be stated.</summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="window">The rows the read returns.</param>
    /// <returns>The plan, or why it cannot be built.</returns>
    /// <remarks>
    /// Virtual and refusing rather than abstract: the generated NotFound sentinel overrides abstract members as public, and this one is
    /// protected. Every style overrides it; the sentinel is refused as a style no option answers to.
    /// </remarks>
    protected virtual IGenericResult<HttpPagingPlan> CreatePlan(HttpPagingSettings settings, HttpReadWindow window)
    {
        return GenericResult<HttpPagingPlan>.Failure(HttpPagingLog.PagingStyleUnknown(settings.Logger, settings.ConnectionName, Name));
    }

    /// <summary>The style's own step: writes a query's window into one request once the required properties are known to be stated.</summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <param name="take">The most rows the query returns; null for all.</param>
    /// <returns>How the request asks for the window, or the refusal.</returns>
    /// <remarks>Virtual and refusing rather than abstract, for the same reason as <see cref="CreatePlan"/>.</remarks>
    protected virtual IGenericResult<HttpPagingWindow> CreateWindow(HttpPagingSettings settings, int skip, int? take)
    {
        return GenericResult<HttpPagingWindow>.Failure(HttpPagingLog.PagingStyleUnknown(settings.Logger, settings.ConnectionName, Name));
    }

    /// <summary>Refuses a window that starts after the first row, for a style that can only start where the remote's pointers start.</summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <returns>Success when the window starts at the first row; otherwise the refusal.</returns>
    protected IGenericResult FromTheStart(HttpPagingSettings settings, int skip)
    {
        return skip == 0
            ? GenericResult.Success()
            : GenericResult.Failure(HttpPagingLog.PagingWindowNotAddressable(
                settings.Logger, settings.ConnectionName, Name, skip, "it follows the remote's own pointers, which start at the first row"));
    }

    /// <summary>
    /// Writes a window for a style that follows the remote's own pointers: it must start at the first row, and the rows it takes are asked for
    /// as the page size when the connection names the parameter for it (and cut by the reader when the remote returns more).
    /// </summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="skip">The rows the query skips.</param>
    /// <param name="take">The most rows the query returns; null for all.</param>
    /// <returns>How the request asks for the window, or the refusal.</returns>
    protected IGenericResult<HttpPagingWindow> SizedFromTheStart(HttpPagingSettings settings, int skip, int? take)
    {
        var fromStart = FromTheStart(settings, skip);
        if (fromStart.IsFailure) return fromStart.ToNewResult<HttpPagingWindow>();

        return GenericResult<HttpPagingWindow>.Success(
            take is int size && Stated(settings, HttpPagingProperties.PageSizeParameter) is { } sizeName
                ? new HttpPagingWindow([(sizeName, size.ToString(CultureInfo.InvariantCulture))], 0, take)
                : new HttpPagingWindow([], 0, take));
    }

    // A window is a count of rows: a negative skip, or fewer rows than the use allows, is refused before the style writes anything (FDW-5009).
    private static IGenericResult Counted(HttpPagingSettings settings, int skip, int? take, int minimumTake)
    {
        if (skip < 0)
            return GenericResult.Failure(HttpPagingLog.PagingSkipNegative(settings.Logger, settings.ConnectionName, skip));

        return take is int rows && rows < minimumTake
            ? GenericResult.Failure(HttpPagingLog.PagingTakeTooSmall(settings.Logger, settings.ConnectionName, rows, minimumTake))
            : GenericResult.Success();
    }

    private IGenericResult RequiredStated(HttpPagingSettings settings)
    {
        foreach (var property in RequiredProperties)
        {
            if (!settings.Values.TryGetValue(property, out var value) || string.IsNullOrWhiteSpace(value))
                return GenericResult.Failure(HttpPagingLog.PagingPropertyMissing(settings.Logger, settings.ConnectionName, Name, property));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Reads a whole-number property: the value the connection states, else the default the host or this style gives. A stated value that is
    /// not a whole number of at least <paramref name="minimum"/> is refused, never replaced.
    /// </summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="property">The property.</param>
    /// <param name="minimum">The smallest value it accepts.</param>
    /// <returns>The number, or the refusal.</returns>
    protected IGenericResult<int> WholeNumber(HttpPagingSettings settings, string property, int minimum)
    {
        if (Stated(settings, property) is { } text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number >= minimum
                ? GenericResult<int>.Success(number)
                : GenericResult<int>.Failure(HttpPagingLog.PagingPropertyNotWholeNumber(settings.Logger, settings.ConnectionName, property, text, minimum));
        }

        return ServiceTypeDefaults.TryResolve(this, property, out var carried) && carried is int declared && declared >= minimum
            ? GenericResult<int>.Success(declared)
            : GenericResult<int>.Failure(HttpPagingLog.PagingDefaultNotDeclared(settings.Logger, settings.ConnectionName, Name, property));
    }

    /// <summary>
    /// Reads the page size a style that follows the remote's links asks for: only when the connection names the query parameter that carries
    /// it (<see cref="HttpPagingProperties.PageSizeParameter"/>); 0, asking for no size, when it names none.
    /// </summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <returns>The size asked for, 0 for none, or the refusal.</returns>
    protected IGenericResult<int> SizeAskedFor(HttpPagingSettings settings)
        => Stated(settings, HttpPagingProperties.PageSizeParameter) is null
            ? GenericResult<int>.Success(0)
            : WholeNumber(settings, HttpPagingProperties.PageSize, 1);

    /// <summary>Gets the value a connection states for a property; null when it states none.</summary>
    /// <param name="settings">The connection's paging settings.</param>
    /// <param name="property">The property.</param>
    /// <returns>The stated value, trimmed; null when it is left out or blank.</returns>
    protected static string? Stated(HttpPagingSettings settings, string property)
    {
        return settings.Values.TryGetValue(property, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
    }
}
