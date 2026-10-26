using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Messaging.Endpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Messaging.Endpoints;

/// <summary>
/// Lets a list of things answer what each of them allows the caller as an alert of one kind (FDW-2431), so a screen that lists failed runs draws each
/// row's actions without asking <c>GET /alerts</c> again.
/// </summary>
/// <remarks>
/// <para>
/// The list is a JSON answer with an <c>items</c> array whose rows each carry an <c>id</c>. Each row's id is asked of the alert kind in the way
/// <c>GET /alerts?alert=kind:id</c> asks it, so the kind says which rows are alerts and which the caller may see. A row that is an alert gets an
/// <c>alert</c> property in the shape of an entry of that answer (<see cref="AlertResponse"/>); a row that is not one is left as it was.
/// </para>
/// <para>
/// An answer that is not such a list, or a row with no id, is a fault of whatever produced it: it fails, and nothing is passed on as though it had been asked.
/// </para>
/// </remarks>
public static class AlertListFields
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string AlertProperty = "alert";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Adds the alert fields of one kind to the rows of a list.</summary>
    /// <param name="alerts">Answers what each alert allows the caller.</param>
    /// <param name="kind">The kind of alert the rows may be, such as <c>ScheduleFailure</c>.</param>
    /// <param name="listJson">The list as JSON: an object with an <c>items</c> array whose rows each carry an <c>id</c>.</param>
    /// <param name="logger">The logger a fault is written to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list with the alert fields added, or the failure of the question, such as a kind that does not exist.</returns>
    public static async Task<IGenericResult<string>> Attach(
        IAlertActionService alerts, string kind, string listJson, ILogger logger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(alerts);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(listJson);
        ArgumentNullException.ThrowIfNull(logger);

        MessagingEndpointLog.TraceAlertEndpoint(logger, "attach to a list");

        var read = Read(kind, listJson, logger);
        if (read.IsFailure)
            return read.ToNewResult<string>();

        var list = read.Value!;
        for (var start = 0; start < list.Rows.Count; start += AlertLimits.MaxAlerts)
        {
            var attached = await AttachTo(alerts, kind, list.Rows.GetRange(start, Math.Min(AlertLimits.MaxAlerts, list.Rows.Count - start)), cancellationToken).ConfigureAwait(false);
            if (attached.IsFailure)
                return attached.ToNewResult<string>();
        }

        return GenericResult<string>.Success(list.Body.ToJsonString());
    }

    // Reads the list: the object, and each row with the identifier it names. An answer that is not such a list, or a row with no identifier, is a fault.
    private static IGenericResult<ReadList> Read(string kind, string listJson, ILogger logger)
    {
        JsonNode? parsed;
        try
        {
            parsed = JsonNode.Parse(listJson);
        }
        catch (JsonException)
        {
            return GenericResult<ReadList>.Failure(MessagingEndpointLog.AlertListNotAList(logger, kind));
        }

        if (parsed is not JsonObject body || body[ItemsProperty] is not JsonArray items)
            return GenericResult<ReadList>.Failure(MessagingEndpointLog.AlertListNotAList(logger, kind));

        var rows = new List<ListRow>();
        foreach (var item in items)
        {
            if (RowOf(item) is not { } row)
                return GenericResult<ReadList>.Failure(MessagingEndpointLog.AlertListRowWithoutId(logger, kind));

            rows.Add(row);
        }

        return GenericResult<ReadList>.Success(new ReadList(body, rows));
    }

    // The row an item is, or nothing when it is not an object with a GUID identifier.
    private static ListRow? RowOf(JsonNode? item)
    {
        if (item is not JsonObject value || value[IdProperty] is not JsonValue idValue || !idValue.TryGetValue<string>(out var text))
            return null;

        return Guid.TryParse(text, CultureInfo.InvariantCulture, out var id) ? new ListRow(value, id) : null;
    }

    // Asks the kind about up to a question's worth of rows, and puts each answer on its row.
    private static async Task<IGenericResult> AttachTo(IAlertActionService alerts, string kind, List<ListRow> chunk, CancellationToken cancellationToken)
    {
        var listed = await alerts.List(chunk.ConvertAll(row => new AlertKey(kind, row.Id)), cancellationToken).ConfigureAwait(false);
        if (listed.IsFailure)
            return listed;

        var byId = new Dictionary<Guid, AlertSummary>();
        foreach (var summary in listed.Value!.Items)
        {
            byId[summary.Id] = summary;
        }

        foreach (var row in chunk)
        {
            if (byId.TryGetValue(row.Id, out var summary))
                row.Body[AlertProperty] = JsonSerializer.SerializeToNode(AlertResponse.Of(summary), Options);
        }

        return GenericResult.Success();
    }

    // A row of the list and the identifier it names.
    private sealed record ListRow(JsonObject Body, Guid Id);

    // The list's object and its rows.
    private sealed record ReadList(JsonObject Body, List<ListRow> Rows);
}
