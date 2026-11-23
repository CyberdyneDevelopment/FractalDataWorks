using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The types of value a row can hold and how much memory a value of each is estimated to hold: text counts two bytes a character, a
/// number its width, a date eight bytes, a byte array its length.
/// </summary>
/// <remarks>A value is found by its exact type through <c>ByClrType</c>; a type that matches none gets <c>NotFound</c> and counts <see cref="RowSize.OtherValue"/> bytes.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RowValueShapeBase), typeof(IRowValueShape), typeof(RowValueShapes))]
public abstract partial class RowValueShapes : TypeCollectionBase<RowValueShapeBase, IRowValueShape>
{
}
