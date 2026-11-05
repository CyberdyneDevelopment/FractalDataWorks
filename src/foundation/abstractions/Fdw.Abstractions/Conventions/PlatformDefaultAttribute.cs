using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Marks a field or property that holds a default the platform declares by name, such as a decimal column's default
/// precision. FDW055 accepts <c>x ?? Platform.DefaultValue</c> when the referenced member carries this attribute.
/// </summary>
/// <remarks>
/// Why on the member: the default is declared once, where it is defined, and every use names it. A member is marked
/// only when its owner confirms it is the platform's declared default (FDW-3580).
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class PlatformDefaultAttribute : Attribute
{
}
