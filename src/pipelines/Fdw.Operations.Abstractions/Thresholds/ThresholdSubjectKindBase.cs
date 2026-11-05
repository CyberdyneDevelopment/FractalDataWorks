using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Base class for the options of <see cref="ThresholdSubjectKinds"/> (FDW-3936), using the CRTP pattern.</summary>
public abstract class ThresholdSubjectKindBase : TypeOptionBase<ThresholdSubjectKindBase>, IThresholdSubjectKind
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdSubjectKindBase"/> class.</summary>
    /// <param name="name">The option name; the kind that <see cref="ThresholdSubject.Kind"/> and <see cref="IThreshold.SubjectKind"/> state.</param>
    /// <param name="description">What the kind is.</param>
    /// <param name="pathName">The path (schema) of the link table.</param>
    /// <param name="containerName">The name of the link table.</param>
    protected ThresholdSubjectKindBase(string name, string description, string pathName, string containerName)
        : base(name, name, description, "ThresholdSubjectKinds")
    {
        PathName = pathName;
        ContainerName = containerName;
    }

    /// <inheritdoc/>
    public string PathName { get; }

    /// <inheritdoc/>
    public string ContainerName { get; }
}
