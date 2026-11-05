using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks the constructor parameter whose argument becomes a type option's <c>Id</c>.
/// </summary>
/// <remarks>
/// Why: the Collections generator refuses two options of one collection that declare the same Id (TC008, FDW-4045). It reads the
/// Id by following each option's constructor chain through the base(...) and this(...) initializers it can see as source. A base
/// compiled into another assembly shows no initializer, so the parameter that carries the Id there says so with this attribute.
/// Without it a duplicate Id surfaced only when the collection froze, at its first use.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeOptionIdAttribute : Attribute
{
}
