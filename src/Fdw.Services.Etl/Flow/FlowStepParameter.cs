using System;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One run parameter an Execute Pipeline step gives the pipeline it starts: its name and how its text value is made from the
/// variables in scope when the step runs (FDW-1403).
/// </summary>
/// <param name="Name">The parameter's name, as the started pipeline declares it.</param>
/// <param name="Value">Makes the value, as text, from the variables in scope.</param>
public sealed record FlowStepParameter(string Name, Func<FlowVariables, string> Value);
