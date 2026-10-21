namespace Fdw.Services.Etl.Graphs;

public sealed partial class PipelineComponentConfiguration
{
    /// <summary>Makes a copy that can have its settings changed without changing the stored component.</summary>
    /// <returns>A shallow copy: its settings are its own, its fields, columns, value sources and stages are shared with the stored component and are only read.</returns>
    public PipelineComponentConfiguration Copy() => (PipelineComponentConfiguration)MemberwiseClone();
}
