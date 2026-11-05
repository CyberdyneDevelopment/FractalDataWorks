using System;
using Fdw.Data.DataSets.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The values a data set request may leave out and the ones it may not. A value its caller chooses (a new source's kind, priority and
/// predicate pushdown, a new field's nullability) has no default: left out, it refuses the request, naming what is missing (FDW-3931). A
/// tuning value (the version, the caching block's values) left out takes the value the data set kind declares, or a host's override of it
/// (FDW-3956).
/// </summary>
/// <remarks>
/// A choice the caller makes is not a default at all: a wrong guess would silently do the wrong thing, so nothing guesses and nothing is
/// seeded for it (FDW-3823 plan revision 3; Mike, 2026-11-04: "in configuration and not hardcoded").
/// </remarks>
internal sealed class DataSetRequestValues
{
    private readonly string _dataSetName;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetRequestValues"/> class.</summary>
    /// <param name="dataSetName">The data set the request names, for the messages.</param>
    /// <param name="logger">The logger.</param>
    internal DataSetRequestValues(string dataSetName, ILogger logger)
    {
        _dataSetName = dataSetName ?? throw new ArgumentNullException(nameof(dataSetName));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>A value the caller chooses: the stated one, else a refusal naming it.</summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="stated">The value the request states.</param>
    /// <param name="part">What the value belongs to, as a person says it ("source", "field").</param>
    /// <param name="partName">The name of what it belongs to.</param>
    /// <param name="field">The value's name, as the request spells it.</param>
    /// <returns>The value, or <c>ValueRequired</c>.</returns>
    internal IGenericResult<T> Required<T>(T? stated, string part, string partName, string field)
        where T : struct =>
        stated is { } value ? GenericResult<T>.Success(value) : GenericResult<T>.Failure(DataSetsResultCodes.ValueRequired, _logger, Missing(part, partName, field));

    /// <summary>Text the caller chooses: the stated text, not blank, else a refusal naming it.</summary>
    /// <param name="stated">The text the request states.</param>
    /// <param name="part">What the value belongs to, as a person says it.</param>
    /// <param name="partName">The name of what it belongs to.</param>
    /// <param name="field">The value's name, as the request spells it.</param>
    /// <returns>The text, or <c>ValueRequired</c>.</returns>
    internal IGenericResult<string> RequiredText(string? stated, string part, string partName, string field) =>
        string.IsNullOrWhiteSpace(stated)
            ? GenericResult<string>.Failure(DataSetsResultCodes.ValueRequired, _logger, Missing(part, partName, field))
            : GenericResult<string>.Success(stated);

    /// <summary>
    /// A tuning value: the stated one, else the value the data set kind declares (or a host's override of it), else a refusal naming it.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="stated">The value the request states.</param>
    /// <param name="implementation">The data set's kind, which declares the default.</param>
    /// <param name="key">The declared default's key, one of <see cref="DataSetDeclaredDefaults"/>.</param>
    /// <param name="field">The value's name, as the request spells it.</param>
    /// <returns>The value, or <c>ValueRequired</c> when the kind declares none.</returns>
    internal IGenericResult<T> Declared<T>(T? stated, string implementation, string key, string field)
        where T : struct =>
        stated is { } value
            ? GenericResult<T>.Success(value)
            : ServiceTypeDefaults.TryResolve(DataSetTypes.ByName(implementation), key, out var declared) && declared is T typed
                ? GenericResult<T>.Success(typed)
                : GenericResult<T>.Failure(DataSetsResultCodes.ValueRequired, _logger, Missing("data set", _dataSetName, field));

    /// <summary>Tuning text: the stated text, not blank, else the value the data set kind declares, else a refusal naming it.</summary>
    /// <param name="stated">The text the request states.</param>
    /// <param name="implementation">The data set's kind, which declares the default.</param>
    /// <param name="key">The declared default's key, one of <see cref="DataSetDeclaredDefaults"/>.</param>
    /// <param name="field">The value's name, as the request spells it.</param>
    /// <returns>The text, or <c>ValueRequired</c> when the kind declares none.</returns>
    internal IGenericResult<string> DeclaredText(string? stated, string implementation, string key, string field) =>
        !string.IsNullOrWhiteSpace(stated)
            ? GenericResult<string>.Success(stated)
            : ServiceTypeDefaults.TryResolve(DataSetTypes.ByName(implementation), key, out var declared) && declared is string { Length: > 0 } text
                ? GenericResult<string>.Success(text)
                : GenericResult<string>.Failure(DataSetsResultCodes.ValueRequired, _logger, Missing("data set", _dataSetName, field));

    private ResultDetails Missing(string part, string partName, string field) =>
        ResultDetails.Create("name", _dataSetName, "part", part, "partName", partName).With("field", field);
}
