// Embedded copy — source generators cannot reference Fdw.Abstractions.
// The convention analyzers match this attribute by name, not by assembly.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

// Why: pure attribute definition (declarative metadata only) — no logic to unit test.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
internal sealed class ConventionOverrideAttribute : Attribute
{
    public int MaxMethodLines { get; set; } = -1;
    public int MaxCyclomaticComplexity { get; set; } = -1;
}
