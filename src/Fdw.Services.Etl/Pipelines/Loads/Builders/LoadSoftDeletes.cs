using Fdw.Commands.Data.SoftDeletes;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using CommandSoftDeleteModes = Fdw.Commands.Data.SoftDeletes.SoftDeleteModes;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>
/// Builds the command layer's soft delete options from a load row (FDW-1109), refusing a mode the kind of load cannot take.
/// </summary>
/// <remarks>
/// A load row names its soft delete mode by text; this is where it becomes the command layer's option, found in the same
/// <c>SoftDeleteModes</c> collection the load validator checks it against.
/// </remarks>
internal static class LoadSoftDeletes
{
    /// <summary>Builds the soft delete options of a load row.</summary>
    /// <param name="input">The load input.</param>
    /// <param name="loadKind">The kind of load, for refusal messages.</param>
    /// <param name="absentAllowed">Whether the kind can flag the keys that are absent from the source.</param>
    /// <param name="markerAllowed">Whether the kind can flag the rows whose marker matches.</param>
    /// <param name="absent">The keys of the source set, for the absent-from-source mode.</param>
    /// <returns>The options, or a failure saying why the row's soft delete cannot be used.</returns>
    public static IGenericResult<SoftDeleteOptions> Create(
        LoadCommandInput input,
        string loadKind,
        bool absentAllowed,
        bool markerAllowed,
        AbsentKeySet? absent)
    {
        var row = input.Row;
        var mode = CommandSoftDeleteModes.ByName(row.SoftDeleteMode!);
        if (ReferenceEquals(mode, CommandSoftDeleteModes.NotFound))
            return Refused(input, row.SoftDeleteMode!, loadKind, "it is not a soft delete mode");

        var absentMode = ReferenceEquals(mode, CommandSoftDeleteModes.AbsentFromSource);
        if (absentMode && !absentAllowed)
            return Refused(input, mode.Name, loadKind, "this kind of load has no source set to compare against");
        if (!absentMode && !markerAllowed)
            return Refused(input, mode.Name, loadKind, "this kind of load takes a source set to compare against, and a marker would ignore it");

        var options = new SoftDeleteOptions(row.DeletedFlagColumn!, mode)
        {
            DeletedAtColumn = row.DeletedAtColumn,
            Marker = absentMode ? null : new SoftDeleteMarker(row.SoftDeleteMarkerColumn!, row.SoftDeleteMarkerValue!),
            AbsentFromSource = absentMode ? absent : null,
        };
        var problem = options.FirstProblem();
        return problem is null
            ? GenericResult<SoftDeleteOptions>.Success(options)
            : GenericResult<SoftDeleteOptions>.Failure(PipelineRunLog.LoadOptionsRefused(input.Logger, input.Owner, loadKind, problem));
    }

    private static IGenericResult<SoftDeleteOptions> Refused(LoadCommandInput input, string mode, string loadKind, string reason)
        => GenericResult<SoftDeleteOptions>.Failure(PipelineRunLog.SoftDeleteModeNotForLoad(input.Logger, input.Owner, mode, loadKind, reason));
}
