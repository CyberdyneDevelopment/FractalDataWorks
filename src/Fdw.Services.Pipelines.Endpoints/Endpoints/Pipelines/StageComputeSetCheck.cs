using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Checks a Compute stage against a Built transformation that reads and writes tables, when the pipeline is saved (FDW-1231):
/// every declared table, output and input is bound exactly once, the sources and routes are well formed, and at most one
/// output replaces the stage's rows. The run checks the same again when it starts, because a transformation can change after
/// the pipeline was saved.
/// </summary>
internal static class StageComputeSetCheck
{
    private const string TablesField = "Compute.Tables";
    private const string OutputsField = "Compute.Outputs";
    private const string ScalarsField = "Compute.Scalars";
    private const string ActionField = "Compute.OnRowFailure";

    internal static List<(string Field, string Reason)> Problems(PipelineCompute compute, BuiltTransformationConfiguration built, ILogger logger)
    {
        var problems = new List<(string Field, string Reason)>();
        if (built.Outputs.Count > 0)
        {
            problems.Add((OutputsField, ComputeStageLog.MixedOutputs(logger, built.Name).Message));
        }

        if (compute.Inputs.Count > 0)
        {
            problems.Add(("Compute.Inputs", ComputeStageLog.RecordMemberInSetMode(logger, built.Name, "field inputs").Message));
        }

        var action = RowFailure(compute, logger);
        if (action is not null)
        {
            problems.Add(action.Value);
        }

        problems.AddRange(Tables(compute, built, logger));
        problems.AddRange(Outputs(compute, built, logger));
        problems.AddRange(Scalars(compute, built, logger));
        return problems;
    }

    private static IEnumerable<(string Field, string Reason)> Tables(PipelineCompute compute, BuiltTransformationConfiguration built, ILogger logger)
    {
        foreach (var repeated in compute.Tables.GroupBy(table => table.Input, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            yield return (TablesField, ComputeStageLog.TableBoundTwice(logger, repeated.Key).Message);
        }

        foreach (var declared in built.TableInputs.Where(declared => !compute.Tables.Any(table => string.Equals(table.Input, declared.Name, StringComparison.Ordinal))))
        {
            yield return (TablesField, ComputeStageLog.TableNotBound(logger, built.Name, declared.Name).Message);
        }

        foreach (var table in compute.Tables)
        {
            if (!built.TableInputs.Any(declared => string.Equals(declared.Name, table.Input, StringComparison.Ordinal)))
            {
                yield return (TablesField, ComputeStageLog.TableNotDeclared(logger, built.Name, table.Input).Message);
            }

            var reason = SourceProblem(table);
            if (reason is not null)
            {
                yield return (TablesField, ComputeStageLog.TableSourceInvalid(logger, table.Input, reason).Message);
            }
        }
    }

    // Stage rows have no name; a data set or a named output has to say which.
    private static string? SourceProblem(PipelineComputeTable table)
    {
        var named = !string.IsNullOrWhiteSpace(table.Name);
        return table.From switch
        {
            "Stage" => named ? "the stage's own rows have no name" : null,
            "DataSet" or "Output" => named ? null : "a data set or output source states its name",
            _ => $"'{table.From}' is not Stage, DataSet or Output",
        };
    }

    private static IEnumerable<(string Field, string Reason)> Outputs(PipelineCompute compute, BuiltTransformationConfiguration built, ILogger logger)
    {
        foreach (var repeated in compute.Outputs.GroupBy(output => output.Output, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            yield return (OutputsField, ComputeStageLog.OutputRoutedTwice(logger, repeated.Key).Message);
        }

        foreach (var declared in built.TableOutputs.Where(declared => !compute.Outputs.Any(output => string.Equals(output.Output, declared.Name, StringComparison.Ordinal))))
        {
            yield return (OutputsField, ComputeStageLog.OutputNotRouted(logger, built.Name, declared.Name).Message);
        }

        foreach (var output in compute.Outputs)
        {
            if (!built.TableOutputs.Any(declared => string.Equals(declared.Name, output.Output, StringComparison.Ordinal)))
            {
                yield return (OutputsField, ComputeStageLog.OutputNotDeclared(logger, built.Name, output.Output).Message);
            }

            var reason = RouteProblem(output);
            if (reason is not null)
            {
                yield return (OutputsField, ComputeStageLog.RouteInvalid(logger, output.Output, reason).Message);
            }
        }

        var replacing = compute.Outputs.Count(output => string.Equals(output.Route, "Replace", StringComparison.Ordinal));
        if (replacing > 1)
        {
            yield return (OutputsField, ComputeStageLog.ManyReplaceRoutes(logger, replacing).Message);
        }
    }

    private static string? RouteProblem(PipelineComputeOutput output)
    {
        var named = !string.IsNullOrWhiteSpace(output.Name);
        return output.Route switch
        {
            "Replace" => named ? "a Replace route has no name" : null,
            "Named" => named ? null : "a Named route states the name it keeps the rows under",
            _ => $"'{output.Route}' is not Replace or Named",
        };
    }

    // What a failing row does is the author's decision: the stage states it, and none is picked here.
    private static (string Field, string Reason)? RowFailure(PipelineCompute compute, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(compute.OnRowFailure))
        {
            return (ActionField, ComputeStageLog.RowFailureNotStated(logger).Message);
        }

        return RowFailureActions.IsKnown(compute.OnRowFailure) ? null : (ActionField, ComputeStageLog.RowFailureUnknown(logger, compute.OnRowFailure).Message);
    }

    private static IEnumerable<(string Field, string Reason)> Scalars(PipelineCompute compute, BuiltTransformationConfiguration built, ILogger logger)
    {
        foreach (var repeated in compute.Scalars.GroupBy(scalar => scalar.Input, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            yield return (ScalarsField, ComputeStageLog.ScalarGivenTwice(logger, repeated.Key).Message);
        }

        foreach (var declared in built.Inputs.Where(declared => !compute.Scalars.Any(scalar => string.Equals(scalar.Input, declared.Name, StringComparison.Ordinal))))
        {
            yield return (ScalarsField, ComputeStageLog.ScalarNotGiven(logger, built.Name, declared.Name).Message);
        }

        foreach (var scalar in compute.Scalars)
        {
            if (!built.Inputs.Any(declared => string.Equals(declared.Name, scalar.Input, StringComparison.Ordinal)))
            {
                yield return (ScalarsField, ComputeStageLog.ScalarNotDeclared(logger, built.Name, scalar.Input).Message);
            }
            else if (!ComputeScalarValue.TryParse(scalar.Value, scalar.ValueType, out _))
            {
                yield return (ScalarsField, ComputeStageLog.ScalarInvalid(logger, scalar.Input, scalar.Value, scalar.ValueType).Message);
            }
        }
    }
}
