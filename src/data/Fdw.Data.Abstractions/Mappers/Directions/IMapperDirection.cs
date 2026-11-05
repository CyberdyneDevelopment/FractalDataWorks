using Fdw.Collections;

namespace Fdw.Data;

/// <summary>
/// Which mapping directions a generated POCO mapper emits: only the read side, only the write side, or both.
/// </summary>
public interface IMapperDirection : ITypeOption<int, MapperDirectionBase>
{
    /// <summary>Gets a one-sentence description of the direction.</summary>
    string Summary { get; }

    /// <summary>Gets whether the mapper emits the read side: <c>MapFromReader</c> and <c>MapFromDictionary</c>.</summary>
    bool EmitsInput { get; }

    /// <summary>Gets whether the mapper emits the write side: <c>GetPropertyNames</c> and <c>MapToParameters</c>.</summary>
    bool EmitsOutput { get; }
}
