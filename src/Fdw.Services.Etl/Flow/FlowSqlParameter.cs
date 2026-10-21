using System;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>A named parameter of an Execute SQL step, whose value is made from the variables when the step runs (FDW-1443).</summary>
/// <param name="Name">The parameter's name.</param>
/// <param name="Value">Makes the value; a reference to one variable keeps that variable's type and null.</param>
public sealed record FlowSqlParameter(string Name, Func<FlowVariables, object?> Value);
