using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads;

/// <summary>
/// Checks the reject steps of a BatchCopy pipeline (FDW-1142): the rows that write the records the pipeline's stages
/// rejected, after its load. A reject step is an ordinary load step in every way <see cref="PipelineLoadValidator"/> judges
/// (kind, destination, columns, value sources), and in three more: its kind can write rejected records, it names the two
/// reserved columns among its write columns, and its destination is not one the pipeline's load writes to.
/// </summary>
/// <remarks>
/// Why a class of its own: <see cref="PipelineLoadValidator"/> judges the rows that load the output; the reject steps are a
/// separate list, ordered from 1 and named apart from the load steps, with rules only they have. The rules are flags on the
/// kind (<see cref="Fdw.Services.Etl.Abstractions.IAcceptsRejectedRecordsShape.AcceptsRejectedRecords"/>), so nothing here branches on a kind's name.
/// </remarks>
public static class PipelineRejectStepValidator
{
    /// <summary>Validates the reject steps of one pipeline.</summary>
    /// <param name="rejectRows">The pipeline's reject steps.</param>
    /// <param name="loadRows">The pipeline's rows that load the output, whose names and destinations the reject steps may not reuse.</param>
    /// <param name="pipelineDestinationDataSet">The data set the pipeline's own load writes to, or <see langword="null"/> when its destination is not a data set.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <returns>Success, or a failure whose message says what is wrong.</returns>
    public static IGenericResult Validate(
        IReadOnlyList<PipelineLoadStepConfiguration> rejectRows,
        IReadOnlyList<PipelineLoadStepConfiguration> loadRows,
        string? pipelineDestinationDataSet,
        ILogger logger)
    {
        if (rejectRows.Count == 0)
            return GenericResult.Success();
        if (rejectRows.Any(row => row.StepOrder is null))
            return GenericResult.Failure(PipelineLoadLog.RejectStepIsOwnLoad(logger));

        var seenOrders = new HashSet<int>();
        var seenNames = new HashSet<string>(loadRows.Select(row => row.Name), StringComparer.OrdinalIgnoreCase);
        var destinations = DestinationsOf(loadRows, pipelineDestinationDataSet);
        foreach (var step in rejectRows)
        {
            var result = ValidateStep(step, rejectRows.Count, seenOrders, seenNames, destinations, logger);
            if (result.IsFailure)
                return result;
        }

        return GenericResult.Success();
    }

    private static IGenericResult ValidateStep(
        PipelineLoadStepConfiguration step,
        int stepCount,
        HashSet<int> seenOrders,
        HashSet<string> seenNames,
        HashSet<string> loadDestinations,
        ILogger logger)
    {
        var identity = PipelineLoadValidator.ValidateStepIdentity(step.StepOrder!.Value, step, stepCount, seenOrders, seenNames, logger);
        if (identity.IsFailure)
            return identity;

        var owner = string.Concat("reject step '", step.Name, "'");
        var accepted = ValidateKindAcceptsRejects(step, owner, logger);
        if (accepted.IsFailure)
            return accepted;

        var structure = PipelineLoadValidator.ValidateStep(step, owner, logger);
        return structure.IsFailure ? structure : ValidateRejectRules(step, owner, loadDestinations, logger);
    }

    // Said before the step's options are judged: a kind that cannot write rejected records is the first thing wrong with the
    // step, whatever its options say. A kind that is missing or unknown is left to the structural check to name.
    private static IGenericResult ValidateKindAcceptsRejects(PipelineLoadStepConfiguration step, string owner, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(step.Kind))
            return GenericResult.Success();

        var kind = LoadStepKinds.ByName(step.Kind);
        if (ReferenceEquals(kind, LoadStepKinds.NotFound) || kind.AcceptsRejectedRecords)
            return GenericResult.Success();

        return GenericResult.Failure(PipelineLoadLog.RejectStepKindRefused(
            logger, owner, kind.Name, string.Join(", ", LoadStepKinds.All().Where(candidate => candidate.AcceptsRejectedRecords).Select(candidate => candidate.Name))));
    }

    private static IGenericResult ValidateRejectRules(
        PipelineLoadStepConfiguration step,
        string owner,
        HashSet<string> loadDestinations,
        ILogger logger)
    {
        if (loadDestinations.Contains(step.DestinationDataSet!))
            return GenericResult.Failure(PipelineLoadLog.RejectDestinationIsLoadDestination(logger, owner, step.DestinationDataSet!));

        var missing = RejectedRecordColumns.All.FirstOrDefault(
            column => !LoadColumns.Writes(step).Contains(column, StringComparer.OrdinalIgnoreCase));
        return missing is null
            ? GenericResult.Success()
            : GenericResult.Failure(PipelineLoadLog.RejectStepColumnMissing(logger, owner, missing));
    }

    // Every data set the pipeline's load writes to: its own destination and the destination of each load step that names one.
    private static HashSet<string> DestinationsOf(IReadOnlyList<PipelineLoadStepConfiguration> loadRows, string? pipelineDestinationDataSet)
    {
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in loadRows.Select(row => row.DestinationDataSet).Append(pipelineDestinationDataSet).Where(name => !string.IsNullOrWhiteSpace(name)))
            destinations.Add(name!);
        return destinations;
    }
}
