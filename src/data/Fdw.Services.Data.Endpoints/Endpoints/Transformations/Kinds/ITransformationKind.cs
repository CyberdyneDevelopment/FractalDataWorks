using Fdw.Collections;

namespace Fdw.Services.Data.Endpoints;

/// <summary>A kind of transformation the <c>/transformations</c> resource creates, changes and reads: Field or Built.</summary>
public interface ITransformationKind : ITypeOption<int, TransformationKindBase>
{
}
