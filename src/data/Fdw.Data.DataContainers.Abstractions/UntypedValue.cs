using System;
using Fdw.Conventions;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// What the platform declares as the CLR type of a value that is null, and so shows no type of its own.
/// </summary>
public static class UntypedValue
{
    /// <summary>Gets the CLR type a schema field takes when the value it was inferred from is null.</summary>
    [PlatformDefault]
    public static Type ClrType { get; } = typeof(object);
}
