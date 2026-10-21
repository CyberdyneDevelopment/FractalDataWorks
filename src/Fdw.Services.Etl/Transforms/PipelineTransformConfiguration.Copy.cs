namespace Fdw.Services.Etl.Transforms;

public sealed partial class PipelineTransformConfiguration
{
    /// <summary>Makes a copy that can have one of its lists replaced without changing the stored stage.</summary>
    /// <returns>A shallow copy: a list the caller replaces is its own, every other child list is shared with the stored stage and only read.</returns>
    public PipelineTransformConfiguration Copy() => (PipelineTransformConfiguration)MemberwiseClone();
}
