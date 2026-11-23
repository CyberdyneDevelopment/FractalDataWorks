using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>A field of a container as a planner sees it (FDW-6969): its name and the CLR type its stated data type reads as.</summary>
/// <param name="Name">The field's name.</param>
/// <param name="ClrType">The CLR type of the field's stated data type; null when the field states none.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record PushdownField(string Name, Type? ClrType);
