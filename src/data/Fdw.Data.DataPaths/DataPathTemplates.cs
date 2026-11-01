using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataPaths;

/// <summary>
/// TypeCollection of <see cref="IDataPathTemplate"/>s. Downstream projects register
/// templates via <c>[TypeOption(typeof(DataPathTemplates), "Name")]</c>. The framework
/// provides zero templates by default.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataPathTemplateBase), typeof(IDataPathTemplate), typeof(DataPathTemplates))]
public abstract partial class DataPathTemplates : TypeCollectionBase<DataPathTemplateBase, IDataPathTemplate>
{
    // Source generator emits ById / ByName / All / RegisterMember.
}
