using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataPaths;

/// <summary>
/// A reusable shape for a DataPath: parameterized template, target DataStore type,
/// default authorization policy, and the list of variables callers must supply.
/// </summary>
/// <remarks>
/// Templates are TypeOptions of <c>DataPathTemplates</c>. Downstream projects register
/// their own via <c>[TypeOption(typeof(DataPathTemplates), "Name")]</c>. A startup
/// hosted service materializes registered templates into <c>data.DataPath</c> rows on
/// matching DataStores (idempotent).
/// </remarks>
public interface IDataPathTemplate : ITypeOption<int, IDataPathTemplate>
{
    /// <summary>The parameterized template (e.g., <c>{userId}/{projectName}/{filename}</c>).</summary>
    string Template { get; }

    /// <summary>The DataStore service-option type this template applies to (e.g., "FileSystem", "Http").</summary>
    string DataStoreServiceType { get; }

    /// <summary>Name of the default <c>IPathAuthorizationPolicy</c> to apply when this template is used.</summary>
    string DefaultPolicyName { get; }

    /// <summary>Names of variables the caller must supply (server-injected variables like tenantId are excluded).</summary>
    IReadOnlyList<string> RequiredVariables { get; }
}
