using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.CalculationNodeOptions;
using Fdw.Services.Calculations.Abstractions.TreeResultCodes;

namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// The JSON form of a calculation tree: the one text the builder, the API, the storage and a future text parser share.
/// </summary>
/// <remarks>
/// A node is <c>{ "op": "Add", "args": [ ... ] }</c>. Field and Ref carry <c>name</c>; Constant carries <c>value</c> or
/// <c>flag</c>; Round carries <c>places</c> and <c>mode</c>; When names its operands <c>if</c>, <c>then</c> and <c>else</c>.
/// Reading is syntactic: it refuses text that is not a tree and an operator that does not exist; the array validator checks
/// the rest.
/// </remarks>
public static class CalculationTreeJson
{
    /// <summary>Writes a tree as JSON text.</summary>
    /// <param name="node">The tree.</param>
    /// <returns>The JSON text.</returns>
    public static string Write(CalculationNode node)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteNode(writer, node);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Reads a tree from JSON text.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The tree, or the refusal.</returns>
    public static IGenericResult<CalculationNode> Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return ReadNode(document.RootElement);
        }
        catch (JsonException exception)
        {
            return Invalid<CalculationNode>(exception.Message);
        }
    }

    private static void WriteNode(Utf8JsonWriter writer, CalculationNode node)
    {
        writer.WriteStartObject();
        writer.WriteString("op", node.Op);
        WriteMembers(writer, node);
        WriteOperands(writer, node);
        writer.WriteEndObject();
    }

    private static void WriteMembers(Utf8JsonWriter writer, CalculationNode node)
    {
        if (node.Name is not null)
        {
            writer.WriteString("name", node.Name);
        }

        if (node.Value is { } value)
        {
            writer.WriteNumber("value", value);
        }

        if (node.Flag is { } flag)
        {
            writer.WriteBoolean("flag", flag);
        }

        if (node.Places is { } places)
        {
            writer.WriteNumber("places", places);
        }

        if (node.Mode is not null)
        {
            writer.WriteString("mode", node.Mode);
        }
    }

    private static void WriteOperands(Utf8JsonWriter writer, CalculationNode node)
    {
        var names = CalculationNodeTypes.ByName(node.Op).OperandNames;
        if (names.Count > 0 && names.Count == node.Operands.Count)
        {
            for (var i = 0; i < names.Count; i++)
            {
                writer.WritePropertyName(names[i]);
                WriteNode(writer, node.Operands[i]);
            }

            return;
        }

        if (node.Operands.Count == 0)
        {
            return;
        }

        writer.WriteStartArray("args");
        foreach (var operand in node.Operands)
        {
            WriteNode(writer, operand);
        }

        writer.WriteEndArray();
    }

    private static IGenericResult<CalculationNode> ReadNode(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return Invalid<CalculationNode>("a node must be a JSON object");
        }

        var op = element.TryGetProperty("op", out var opElement) && opElement.ValueKind == JsonValueKind.String ? opElement.GetString() : null;
        if (op is null || string.IsNullOrWhiteSpace(op))
        {
            return Invalid<CalculationNode>("a node needs an 'op' text");
        }

        var option = CalculationNodeTypes.ByName(op);
        if (ReferenceEquals(option, CalculationNodeTypes.NotFound))
        {
            return GenericResult<CalculationNode>.Failure(
                CalculationTreeResultCodes.ByName("NodeOperatorUnknown"),
                ResultDetails.Create("node", op, "reason", $"'{op}' is not a calculation operator"));
        }

        return ReadParts(element, op, option);
    }

    private static IGenericResult<CalculationNode> ReadParts(JsonElement element, string op, ICalculationNodeType option)
    {
        var members = ReadMembers(element);
        if (members.IsFailure)
        {
            return members.ToNewResult<CalculationNode>();
        }

        var operands = ReadOperands(element, option);
        if (operands.IsFailure)
        {
            return operands.ToNewResult<CalculationNode>();
        }

        var parts = members.Value!;
        return GenericResult<CalculationNode>.Success(
            new CalculationNode(op, parts.Name, parts.Value, parts.Flag, parts.Places, parts.Mode, operands.Value!));
    }

    private static IGenericResult<CalculationNodeMembers> ReadMembers(JsonElement element)
    {
        var members = new CalculationNodeMembers();
        if (element.TryGetProperty("name", out var name))
        {
            members.Name = name.ValueKind == JsonValueKind.String ? name.GetString() : null;
            if (members.Name is null)
            {
                return Invalid<CalculationNodeMembers>("'name' must be text");
            }
        }

        if (element.TryGetProperty("mode", out var mode))
        {
            members.Mode = mode.ValueKind == JsonValueKind.String ? mode.GetString() : null;
            if (members.Mode is null)
            {
                return Invalid<CalculationNodeMembers>("'mode' must be text");
            }
        }

        return ReadNumbers(element, members);
    }

    private static IGenericResult<CalculationNodeMembers> ReadNumbers(JsonElement element, CalculationNodeMembers members)
    {
        if (element.TryGetProperty("value", out var value))
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number))
            {
                return Invalid<CalculationNodeMembers>("'value' must be an exact decimal number");
            }

            members.Value = number;
        }

        if (element.TryGetProperty("places", out var places))
        {
            if (places.ValueKind != JsonValueKind.Number || !places.TryGetInt32(out var count))
            {
                return Invalid<CalculationNodeMembers>("'places' must be a whole number");
            }

            members.Places = count;
        }

        return ReadFlag(element, members);
    }

    private static IGenericResult<CalculationNodeMembers> ReadFlag(JsonElement element, CalculationNodeMembers members)
    {
        if (element.TryGetProperty("flag", out var flag))
        {
            if (flag.ValueKind != JsonValueKind.True && flag.ValueKind != JsonValueKind.False)
            {
                return Invalid<CalculationNodeMembers>("'flag' must be true or false");
            }

            members.Flag = flag.GetBoolean();
        }

        return GenericResult<CalculationNodeMembers>.Success(members);
    }

    private static IGenericResult<IReadOnlyList<CalculationNode>> ReadOperands(JsonElement element, ICalculationNodeType option)
    {
        var names = option.OperandNames;
        var operands = new List<CalculationNode>();
        if (names.Count > 0)
        {
            foreach (var name in names)
            {
                if (!element.TryGetProperty(name, out var child))
                {
                    return Invalid<IReadOnlyList<CalculationNode>>($"a {option.Name} needs '{name}'");
                }

                var node = ReadNode(child);
                if (node.IsFailure)
                {
                    return node.ToNewResult<IReadOnlyList<CalculationNode>>();
                }

                operands.Add(node.Value!);
            }

            return GenericResult<IReadOnlyList<CalculationNode>>.Success(operands);
        }

        return ReadArgs(element, operands);
    }

    private static IGenericResult<IReadOnlyList<CalculationNode>> ReadArgs(JsonElement element, List<CalculationNode> operands)
    {
        if (!element.TryGetProperty("args", out var args))
        {
            return GenericResult<IReadOnlyList<CalculationNode>>.Success(operands);
        }

        if (args.ValueKind != JsonValueKind.Array)
        {
            return Invalid<IReadOnlyList<CalculationNode>>("'args' must be an array");
        }

        foreach (var item in args.EnumerateArray())
        {
            var node = ReadNode(item);
            if (node.IsFailure)
            {
                return node.ToNewResult<IReadOnlyList<CalculationNode>>();
            }

            operands.Add(node.Value!);
        }

        return GenericResult<IReadOnlyList<CalculationNode>>.Success(operands);
    }

    private static IGenericResult<T> Invalid<T>(string reason) =>
        GenericResult<T>.Failure(
            CalculationTreeResultCodes.ByName("TreeJsonInvalid"),
            ResultDetails.Create("node", "json", "reason", reason));
}
