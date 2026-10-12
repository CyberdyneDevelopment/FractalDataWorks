using Fdw.Collections;

namespace Fdw.Data.RowSources.Http.Abstractions;

/// <summary>
/// Interface for REST pagination styles.
/// </summary>
public interface IRestPaginationStyle : ITypeOption<int, RestPaginationStyleBase> { }
