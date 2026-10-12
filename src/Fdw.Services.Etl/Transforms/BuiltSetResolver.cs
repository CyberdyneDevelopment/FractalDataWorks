using System;
using System.Linq;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Runtime;
using Fdw.Services.Calculations.Sets;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Checks a Compute stage against a Built transformation that reads and writes tables, and compiles it, when the run starts
/// (FDW-1231). Every binding the stage states has to match what the transformation declares; nothing is bound by default.
/// </summary>
internal static class BuiltSetResolver
{
    internal static IGenericResult Resolve(
        string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, BuiltCalculationResolution resolution)
    {
        var checkedStage = Check(stageName, stage, built, resolution.Logger);
        if (checkedStage.IsFailure)
        {
            return checkedStage;
        }

        var limits = BuiltCalculationResolver.ReadLimits(stageName, resolution);
        if (limits.IsFailure)
        {
            return limits;
        }

        var setLimits = BuiltCalculationResolver.ReadSetLimits(stageName, resolution);
        if (setLimits.IsFailure)
        {
            return setLimits;
        }

        var definition = BuiltTransformationSetReader.ToDefinition(built, limits.Value!);
        var compiled = definition.IsFailure
            ? definition.ToNewResult<ICompiledCalculationSet>()
            : new CalculationSetCompiler().Compile(definition.Value!, limits.Value!, setLimits.Value!);
        if (compiled.IsFailure)
        {
            ComputeLog.CalculationRefused(resolution.Logger, stageName, built.Name);
            return compiled;
        }

        resolution.CompiledSets[stage.Id] = compiled.Value!;
        ComputeSetLog.SetCompiled(resolution.Logger, stageName, built.Name, built.TableOutputs.Count);
        return GenericResult.Success();
    }

    // The stage's members for reading one record at a time do not apply to a transformation that writes tables.
    internal static IGenericResult CheckRecordMode(string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, ILogger logger)
    {
        var stated = stage.ComputeTables.Count > 0 ? "table bindings" : stage.ComputeOutputs.Count > 0 ? "output routes" : stage.ComputeScalars.Count > 0 ? "single values" : stage.ComputeOnRowFailure is not null ? "onRowFailure" : null;
        return stated is null
            ? GenericResult.Success()
            : GenericResult.Failure(ComputeSetLog.SetMemberInRecordMode(logger, stageName, stated, built.Name));
    }

    private static IGenericResult Check(string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, ILogger logger)
    {
        if (built.Outputs.Count > 0)
        {
            return GenericResult.Failure(ComputeSetLog.MixedOutputs(logger, built.Name));
        }

        if (stage.ComputeInputs.Count > 0)
        {
            return GenericResult.Failure(ComputeSetLog.RecordMemberInSetMode(logger, stageName, "field inputs", built.Name));
        }

        var action = RowFailure(stageName, stage, logger);
        if (action.IsFailure)
        {
            return action;
        }

        var problem = Scalars(stageName, stage, built, logger);
        if (problem.IsFailure)
        {
            return problem;
        }

        var tables = Tables(stageName, stage, built, logger);
        return tables.IsFailure ? tables : Outputs(stageName, stage, built, logger);
    }

    // A failing row is the author's decision: the stage states it and the run never picks one.
    private static IGenericResult RowFailure(string stageName, PipelineTransformConfiguration stage, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(stage.ComputeOnRowFailure))
        {
            return GenericResult.Failure(ComputeSetLog.RowFailureNotStated(logger, stageName));
        }

        return RowFailureActions.IsKnown(stage.ComputeOnRowFailure)
            ? GenericResult.Success()
            : GenericResult.Failure(ComputeSetLog.RowFailureUnknown(logger, stageName, stage.ComputeOnRowFailure));
    }

    private static IGenericResult Scalars(string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, ILogger logger)
    {
        var missing = built.Inputs.FirstOrDefault(declared => !stage.ComputeScalars.Any(given => string.Equals(given.InputName, declared.Name, StringComparison.Ordinal)));
        if (missing is not null)
        {
            return GenericResult.Failure(ComputeSetLog.ScalarNotGiven(logger, stageName, missing.Name, built.Name));
        }

        var unknown = stage.ComputeScalars.FirstOrDefault(given => !built.Inputs.Any(declared => string.Equals(declared.Name, given.InputName, StringComparison.Ordinal)));
        return unknown is null
            ? GenericResult.Success()
            : GenericResult.Failure(ComputeSetLog.ScalarNotDeclared(logger, stageName, unknown.InputName, built.Name));
    }

    private static IGenericResult Tables(string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, ILogger logger)
    {
        var missing = built.TableInputs.FirstOrDefault(declared => !stage.ComputeTables.Any(bound => string.Equals(bound.TableName, declared.Name, StringComparison.Ordinal)));
        if (missing is not null)
        {
            return GenericResult.Failure(ComputeSetLog.TableNotBound(logger, stageName, missing.Name, built.Name));
        }

        var unknown = stage.ComputeTables.FirstOrDefault(bound => !built.TableInputs.Any(declared => string.Equals(declared.Name, bound.TableName, StringComparison.Ordinal)));
        return unknown is null ? Sources(stageName, stage, logger) : GenericResult.Failure(ComputeSetLog.TableNotDeclared(logger, stageName, unknown.TableName, built.Name));
    }

    // A table's rows come from the stage, a named data set or a named output: the last two have to say which.
    private static IGenericResult Sources(string stageName, PipelineTransformConfiguration stage, ILogger logger)
    {
        foreach (var bound in stage.ComputeTables)
        {
            var named = !string.IsNullOrWhiteSpace(bound.FromName);
            var reason = bound.FromKind switch
            {
                "Stage" => named ? "the stage's own rows have no name" : null,
                "DataSet" or "Output" => named ? null : "a data set or output source states its name",
                _ => $"'{bound.FromKind}' is not Stage, DataSet or Output",
            };
            if (reason is not null)
            {
                return GenericResult.Failure(ComputeSetLog.TableSourceInvalid(logger, stageName, bound.TableName, reason));
            }
        }

        return GenericResult.Success();
    }

    private static IGenericResult Outputs(string stageName, PipelineTransformConfiguration stage, BuiltTransformationConfiguration built, ILogger logger)
    {
        var missing = built.TableOutputs.FirstOrDefault(declared => !stage.ComputeOutputs.Any(routed => string.Equals(routed.OutputName, declared.Name, StringComparison.Ordinal)));
        if (missing is not null)
        {
            return GenericResult.Failure(ComputeSetLog.OutputNotRouted(logger, stageName, missing.Name, built.Name));
        }

        var unknown = stage.ComputeOutputs.FirstOrDefault(routed => !built.TableOutputs.Any(declared => string.Equals(declared.Name, routed.OutputName, StringComparison.Ordinal)));
        if (unknown is not null)
        {
            return GenericResult.Failure(ComputeSetLog.OutputNotDeclared(logger, stageName, unknown.OutputName, built.Name));
        }

        return Routes(stageName, stage, logger);
    }

    private static IGenericResult Routes(string stageName, PipelineTransformConfiguration stage, ILogger logger)
    {
        foreach (var routed in stage.ComputeOutputs)
        {
            var named = !string.IsNullOrWhiteSpace(routed.RouteName);
            var reason = routed.Route switch
            {
                "Replace" => named ? "a Replace route has no name" : null,
                "Named" => named ? null : "a Named route states the name it keeps the rows under",
                _ => $"'{routed.Route}' is not Replace or Named",
            };
            if (reason is not null)
            {
                return GenericResult.Failure(ComputeSetLog.RouteInvalid(logger, stageName, routed.OutputName, reason));
            }
        }

        var replacing = stage.ComputeOutputs.Count(routed => string.Equals(routed.Route, "Replace", StringComparison.Ordinal));
        return replacing <= 1 ? GenericResult.Success() : GenericResult.Failure(ComputeSetLog.ManyReplaceRoutes(logger, stageName, replacing));
    }
}
