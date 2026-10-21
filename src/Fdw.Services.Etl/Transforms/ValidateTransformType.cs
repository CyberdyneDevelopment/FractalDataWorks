using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Predicates;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that checks every record against named conditions (FDW-1120).
/// </summary>
/// <remarks>
/// <para>
/// A Validate stage has one or more checks, each a name and a condition in the pipeline's condition language (see
/// <see cref="RecordPredicates"/>), and a failing action. A record is asked the checks in order and stops at the first it
/// fails. With <c>Reject</c> the record leaves the stream and is reported with the name of the check it failed, counting
/// toward the pipeline's maximum error count; with <c>Fail</c> the run fails at the first failing record.
/// </para>
/// <para>
/// Why no default: the stage states what a failing record causes and the checks it asks. A condition that cannot be read
/// is refused when the pipeline is saved and again when the run starts, never treated as a check every record passes.
/// Set-level checks (unique values, row counts) stay in the Quality domain: they need the whole data set.
/// </para>
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Validate")]
public sealed class ValidateTransformType : TransformTypeBase
{
    private const string Kind = "Validate";

    /// <inheritdoc />
    /// <remarks>A Validate that rejects sends the failing records to the component's error output in a graph; one that fails the run rejects none.</remarks>
    public override bool RejectsRecords(IGenericConfiguration configuration) =>
        configuration is PipelineTransformConfiguration { OnFail: { Length: > 0 } onFail }
        && !ReferenceEquals(ValidateFailActions.ByName(onFail), ValidateFailActions.NotFound)
        && !ValidateFailActions.ByName(onFail).StopsRun;

    private static readonly StageMember[] OwnedMembers =
    [
        new("checks", "CheckList", true, "The checks a record must pass, each a name and a condition; may reference {{run parameters}}"),
        new("onFail", "ValidateFailAction", true, "What a failing record causes: Reject leaves the stream and is reported, Fail stops the run")
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateTransformType"/> class.
    /// </summary>
    public ValidateTransformType() : base(
        name: Kind,
        displayName: "Record Validation",
        description: "Checks every record against named conditions - a failing record is rejected or fails the run",
        category: "Validation",
        modifiesStructure: false,
        canFilterRecords: true,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));
        }

        var checks = ValidateChecks.Prepare(config, context);
        if (checks.IsFailure)
        {
            return Task.FromResult(checks.ToNewResult<IDictionary<string, object?>>());
        }

        var failed = checks.Value!.FirstFailed(input);
        if (failed is null)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(input));
        }

        if (checks.Value!.FailAction.StopsRun)
        {
            return Task.FromResult(GenericResult<IDictionary<string, object?>>.Failure(
                EtlResultCodes.ByName("RecordFailedValidation"),
                context.Logger,
                ResultDetails.Create("CheckName", failed, "StageName", config.Name)));
        }

        context.Reject(config.Name, Rejection(config.Name, failed), input);
        return Task.FromResult(GenericResult<IDictionary<string, object?>>.Success(null!));
    }

    /// <inheritdoc />
    public override Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default)
    {
        if (configuration is not PipelineTransformConfiguration config)
        {
            return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));
        }

        var checks = ValidateChecks.Prepare(config, context);
        if (checks.IsFailure)
        {
            return Task.FromResult(checks.ToNewResult<IEnumerable<IDictionary<string, object?>>>());
        }

        var kept = new List<IDictionary<string, object?>>();
        var inCount = 0;
        foreach (var record in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inCount++;
            var failed = checks.Value!.FirstFailed(record);
            if (failed is null)
            {
                kept.Add(record);
            }
            else if (checks.Value!.FailAction.StopsRun)
            {
                return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                    EtlResultCodes.ByName("RecordFailedValidation"),
                    context.Logger,
                    ResultDetails.Create("CheckName", failed, "StageName", config.Name)));
            }
            else
            {
                context.Reject(config.Name, Rejection(config.Name, failed), record);
            }
        }

        TransformLog.ValidateApplied(context.Logger, config.Name, inCount, inCount - kept.Count);
        return Task.FromResult(GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(kept));
    }

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformCheckingSpec checking)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request does not state checks"));
        }

        var problem = FirstProblem(checking);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        foreach (var check in checking.Checks)
        {
            var read = RecordPredicates.Parse(check.Expression, logger);
            if (read.IsFailure)
            {
                return read;
            }
        }

        config.OnFail = checking.OnFail;
        config.Checks = checking.Checks
            .Select((check, index) => new PipelineTransformCheckConfiguration
            {
                Name = check.Name,
                Expression = check.Expression,
                Ordinal = index + 1
            })
            .ToList();

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static string Rejection(string stageName, string checkName) =>
        string.Concat("Record rejected by check '", checkName, "' of stage '", stageName, "'");

    private static string? FirstProblem(ITransformCheckingSpec checking) => ActionProblem(checking) ?? ChecksProblem(checking);

    private static string? ActionProblem(ITransformCheckingSpec checking)
    {
        if (string.IsNullOrWhiteSpace(checking.OnFail))
        {
            return "a validate states what a failing record causes (onFail)";
        }

        return ReferenceEquals(ValidateFailActions.ByName(checking.OnFail), ValidateFailActions.NotFound)
            ? $"onFail '{checking.OnFail}' is not a validate failing action"
            : null;
    }

    private static string? ChecksProblem(ITransformCheckingSpec checking)
    {
        if (checking.Checks.Count == 0)
        {
            return "a validate names at least one check";
        }

        if (checking.Checks.Any(check => string.IsNullOrWhiteSpace(check.Name)))
        {
            return "every check is named";
        }

        var twice = checking.Checks.GroupBy(check => check.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
        if (twice is not null)
        {
            return $"check '{twice.Key}' is listed twice";
        }

        var blank = checking.Checks.FirstOrDefault(check => string.IsNullOrWhiteSpace(check.Expression));
        return blank is null ? null : $"check '{blank.Name}' has no condition";
    }
}
