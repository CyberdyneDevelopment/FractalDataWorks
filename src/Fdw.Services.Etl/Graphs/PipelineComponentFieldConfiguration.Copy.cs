namespace Fdw.Services.Etl.Graphs;

public sealed partial class PipelineComponentFieldConfiguration
{
    /// <summary>Makes a copy that can have its constant changed without changing the stored mapping (FDW-2119).</summary>
    /// <returns>A shallow copy: its values are its own, its transformation chain is shared with the stored mapping and is only read.</returns>
    public PipelineComponentFieldConfiguration Copy() => (PipelineComponentFieldConfiguration)MemberwiseClone();
}
