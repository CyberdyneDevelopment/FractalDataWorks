using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>A kind of owner of a data configuration rule (FDW-6454).</summary>
public interface IDataOptionOwnerKind : ITypeOption<int, DataOptionOwnerKindBase>
{
    /// <summary>Gets the name a console shows for this option (FDW-6342); every option is a type option, which states one.</summary>
    string DisplayName { get; }
}
