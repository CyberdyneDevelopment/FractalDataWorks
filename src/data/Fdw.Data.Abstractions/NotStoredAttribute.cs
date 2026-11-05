using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data;

/// <summary>
/// Marks a settable property of a <c>[GenerateMapper]</c> type that the type holds but does not store: the mapper makes it no column and no child
/// table, so a read leaves it as the type initialises it and a save writes nothing for it.
/// </summary>
/// <remarks>
/// For an implementation that has to carry a list because an interface it implements requires one, and owns no rows of that list. A data set
/// variation varies another data set, so it carries <c>Fields</c>, <c>Sources</c>, <c>Filters</c> and <c>KeyFields</c> empty (FDW-3873). Without this the mapper
/// treats each list as a child table the implementation's table must have a foreign key from, and a read of the whole record is refused.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class NotStoredAttribute : Attribute
{
}
