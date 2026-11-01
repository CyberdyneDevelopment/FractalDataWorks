using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>Builders the flow tests share: sources of numbered rows, collecting destinations, scripted components and graphs.</summary>
internal static class FlowTestKit
{
    /// <summary>A component with one input and one output that sends every batch on as it reads it.</summary>
    public static ScriptedComponent Pass(string name) =>
        new(name, ["input"], ["output"], async (ports, ct) =>
        {
            var input = ports.Input("input");
            var output = ports.Output("output");
            while (await input.Read(ct) is { } batch)
            {
                await output.Write(batch, ct);
            }

            return GenericResult.Success();
        });

    /// <summary>A pass-through component whose input maps its rows (FDW-2013).</summary>
    public static FlowMappedComponent Mapped(string name, params FlowFieldMap[] maps) =>
        new(Pass(name), new Dictionary<string, IReadOnlyList<FlowFieldMap>> { ["input"] = maps });

    public static IDictionary<string, object?> Row(int id, string region = "A") =>
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Id"] = id, ["Region"] = region };

    public static async IAsyncEnumerable<IDictionary<string, object?>> Numbered(
        int count, Func<int, string> region, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return Row(i, region(i));
            await Task.Yield();
        }
    }

    public static FlowSource Source(string name, int count, int batchSize = 3, Func<int, string>? region = null) =>
        new(name, ct => Numbered(count, region ?? (i => i % 2 == 0 ? "A" : "B"), ct), batchSize);

    public static IDictionary<string, object?> Fields(params (string Name, object? Value)[] fields)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in fields)
        {
            row[name] = value;
        }

        return row;
    }

    public static async IAsyncEnumerable<IDictionary<string, object?>> Yield(
        IReadOnlyList<IDictionary<string, object?>> rows, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }

    public static FlowSource FromRows(string name, params IDictionary<string, object?>[] rows) =>
        new(name, ct => Yield(rows, ct), batchSize: 2);

    public static (FlowDestination Component, List<IDictionary<string, object?>> Rows) Sink(string name)
    {
        var rows = new List<IDictionary<string, object?>>();
        var gate = new object();
        var component = new FlowDestination(name, (batch, _) =>
        {
            lock (gate)
            {
                rows.AddRange(batch.Rows);
            }

            return Task.FromResult<IGenericResult>(GenericResult.Success());
        });
        return (component, rows);
    }

    public static FlowLink Link(string from, string to, string output = "output", string input = "input") => new(from, output, to, input);

    public static FlowGraph Graph(IEnumerable<IFlowComponent> components, IEnumerable<FlowLink> links, params FlowDroppedOutput[] dropped) =>
        new([.. components], [.. links], dropped);

    public static FlowRunOptions Options(int capacity = 2) => new() { LinkCapacity = capacity };

    public static int[] Ids(IEnumerable<IDictionary<string, object?>> rows) => [.. rows.Select(r => (int)r["Id"]!)];
}

/// <summary>A component whose behaviour a test writes inline.</summary>
internal sealed class ScriptedComponent : IFlowComponent
{
    private readonly Func<IFlowPorts, CancellationToken, Task<IGenericResult>> _run;

    public ScriptedComponent(
        string name,
        IReadOnlyList<string> inputs,
        IReadOnlyList<string> outputs,
        Func<IFlowPorts, CancellationToken, Task<IGenericResult>> run)
    {
        Name = name;
        Inputs = inputs;
        Outputs = outputs;
        _run = run;
    }

    public string Name { get; }

    public IReadOnlyList<string> Inputs { get; }

    public IReadOnlyList<string> Outputs { get; }

    public Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default) => _run(ports, cancellationToken);

    /// <summary>A component with one input and one output that does nothing.</summary>
    public static ScriptedComponent Idle(string name) =>
        new(name, ["input"], ["output"], (_, _) => Task.FromResult<IGenericResult>(GenericResult.Success()));
}
