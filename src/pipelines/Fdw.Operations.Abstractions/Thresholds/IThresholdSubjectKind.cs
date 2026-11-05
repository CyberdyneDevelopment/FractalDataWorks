using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A kind of subject a threshold can be attached to (FDW-3936): where its link table lives.</summary>
public interface IThresholdSubjectKind : ITypeOption<int, IThresholdSubjectKind>
{
    /// <summary>Gets the path (schema) of the kind's threshold link table.</summary>
    string PathName { get; }

    /// <summary>Gets the name of the kind's threshold link table.</summary>
    string ContainerName { get; }
}
