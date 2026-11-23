using Fdw.Collections;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A description sub-type, a text about a thing: what the thing's own description says, or another text a domain adds. Part of a description's key.</summary>
public interface IDescriptionSubType : ITypeOption<int, DescriptionSubTypeBase>
{
}
