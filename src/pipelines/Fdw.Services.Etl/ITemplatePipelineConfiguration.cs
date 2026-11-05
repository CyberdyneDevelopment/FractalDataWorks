using System;

namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline implementation that can be a template, or can say which template it was made from (FDW-3379): a reader asks for the fact without asking what the
/// engine is, as it does for <see cref="IParallelRunPipelineConfiguration"/>.
/// </summary>
/// <remarks>
/// A template is an ordinary pipeline marked as one. It is never run, and a pipeline made from it is a separate pipeline that records where it came from.
/// </remarks>
public interface ITemplatePipelineConfiguration
{
    /// <summary>Gets a value indicating whether the pipeline is a template: it is never triggered or scheduled.</summary>
    bool IsTemplate { get; }

    /// <summary>Gets the name of the template the pipeline was made from; <see langword="null"/> when it was not made from one.</summary>
    string? TemplateName { get; }

    /// <summary>Gets the version of that template the pipeline was made from, the time the template was last changed; <see langword="null"/> when it was not made from one.</summary>
    DateTimeOffset? TemplateVersion { get; }
}
