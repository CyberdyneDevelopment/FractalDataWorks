using System;

namespace Fdw.Protocols.Tds;

/// <summary>
/// One parameter of an RPC request: its name, whether it is an output, its type and its value.
/// </summary>
public sealed class RpcParameter
{
    /// <summary>Initializes a new instance of the <see cref="RpcParameter"/> class.</summary>
    /// <param name="name">The parameter's name, with its leading @, or empty for a positional one.</param>
    /// <param name="isOutput">Whether the caller passed it by reference to receive a value back.</param>
    /// <param name="typeInfo">The type the client sent it as.</param>
    /// <param name="value">The value; null for a NULL.</param>
    public RpcParameter(string name, bool isOutput, TdsTypeInfo typeInfo, object? value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsOutput = isOutput;
        TypeInfo = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        Value = value;
    }

    /// <summary>Gets the parameter's name, with its leading @, or empty for a positional one.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether the caller passed it by reference to receive a value back.</summary>
    public bool IsOutput { get; }

    /// <summary>Gets the type the client sent it as.</summary>
    public TdsTypeInfo TypeInfo { get; }

    /// <summary>Gets the value; null for a NULL.</summary>
    public object? Value { get; }
}
