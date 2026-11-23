using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// The shapes a stored run time can have: a DateTimeOffset, and a DateTime, which is read as UTC.
/// </summary>
/// <remarks>A value is read by the shape that matches it; a value no shape matches is not a run time, and the reader refuses it.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(RunTimeShapeBase), typeof(IRunTimeShape), typeof(RunTimeShapes))]
public abstract partial class RunTimeShapes : TypeCollectionBase<RunTimeShapeBase, IRunTimeShape>
{
}
