namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// Declares where an option's middleware sits in the request pipeline.
/// </summary>
/// <remarks>
/// Separate from <see cref="IHostType"/> so the collection can sort without every option being
/// forced to care: an option that does not implement this runs after those that do.
/// </remarks>
public interface IHostPipelinePosition
{
    /// <summary>Gets the position. Lower runs earlier.</summary>
    int PipelinePosition { get; }
}
