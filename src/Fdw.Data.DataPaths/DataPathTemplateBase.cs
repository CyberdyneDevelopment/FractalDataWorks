using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataPaths;

/// <summary>Base class for <see cref="IDataPathTemplate"/> TypeOptions.</summary>
public abstract class DataPathTemplateBase : TypeOptionBase<int, DataPathTemplateBase>, IDataPathTemplate
{
    /// <summary>Initializes a new template.</summary>
    protected DataPathTemplateBase(
        int id,
        string name,
        string template,
        string dataStoreServiceType,
        string defaultPolicyName,
        IReadOnlyList<string> requiredVariables)
        : base(id, name)
    {
        Template = template;
        DataStoreServiceType = dataStoreServiceType;
        DefaultPolicyName = defaultPolicyName;
        RequiredVariables = requiredVariables;
    }

    /// <inheritdoc />
    public string Template { get; }

    /// <inheritdoc />
    public string DataStoreServiceType { get; }

    /// <inheritdoc />
    public string DefaultPolicyName { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredVariables { get; }
}
