using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The members of an update body that change a saved pipeline, held in one place so the rule that an update must state
/// something, its refusal message and the list of members that message names cannot drift apart (FDW-1146).
/// </summary>
/// <remarks>
/// Why the list is data and not a hand-written message: the refusal used to name the updateable members in text that was
/// edited separately from the test, and it fell behind when the reject steps were added. Every member an update may change
/// is a row here; the test that states one member at a time fails when a member is added to the request and not to the list.
/// The three parts of a data store source are each a row (FDW-1103): stating any of them replaces the source.
/// </remarks>
public static class UpdatePipelineChanges
{
    /// <summary>The error code the update validator gives a body that states nothing to change. Checking a saved pipeline as it stands accepts that body, so it recognizes the failure by this code.</summary>
    public const string NothingStatedCode = "NothingToUpdate";

    private static readonly (string Name, Func<UpdatePipelineRequest, bool> Stated)[] Members =
    [
        (nameof(UpdatePipelineRequest.SourceDataSet), request => request.SourceDataSet is not null),
        (nameof(UpdatePipelineRequest.SourceDataStoreName), request => request.SourceDataStoreName is not null),
        (nameof(UpdatePipelineRequest.SourceDataStorePath), request => request.SourceDataStorePath is not null),
        (nameof(UpdatePipelineRequest.SourceContainerName), request => request.SourceContainerName is not null),
        (nameof(UpdatePipelineRequest.DestinationDataSet), request => request.DestinationDataSet is not null),
        (nameof(UpdatePipelineRequest.DestinationDataStoreName), request => request.DestinationDataStoreName is not null),
        (nameof(UpdatePipelineRequest.DestinationDataStorePath), request => request.DestinationDataStorePath is not null),
        (nameof(UpdatePipelineRequest.DestinationContainerName), request => request.DestinationContainerName is not null),
        (nameof(UpdatePipelineRequest.Description), request => request.Description is not null),
        (nameof(UpdatePipelineRequest.IsEnabled), request => request.IsEnabled.HasValue),
        (nameof(UpdatePipelineRequest.TruncateBeforeLoad), request => request.TruncateBeforeLoad.HasValue),
        (nameof(UpdatePipelineRequest.Transforms), request => request.Transforms is not null),
        (nameof(UpdatePipelineRequest.LoadMode), request => request.LoadMode is not null),
        (nameof(UpdatePipelineRequest.LoadOptions), request => request.LoadOptions is not null),
        (nameof(UpdatePipelineRequest.LoadSteps), request => request.LoadSteps is not null),
        (nameof(UpdatePipelineRequest.RejectSteps), request => request.RejectSteps is not null),
        (nameof(UpdatePipelineRequest.ValueSources), request => request.ValueSources is not null),
        (nameof(UpdatePipelineRequest.Parameters), request => request.Parameters is not null),
    ];

    /// <summary>Gets the names of the members an update may change, in the order the refusal lists them.</summary>
    public static IReadOnlyList<string> Names { get; } = [.. Members.Select(member => member.Name)];

    /// <summary>Gets the refusal message for a body that states no member to change, naming every member that could be stated.</summary>
    public static string RefusalMessage { get; } =
        string.Concat("At least one updateable field must be supplied (", string.Join(", ", Names), ")");

    /// <summary>Gets whether the body states at least one member to change.</summary>
    /// <param name="request">The update body.</param>
    /// <returns><see langword="true"/> when the body states any member on the list.</returns>
    public static bool Stated(UpdatePipelineRequest request) => Members.Any(member => member.Stated(request));

    /// <summary>Gets whether the body states the one named member.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="name">A name from <see cref="Names"/>.</param>
    /// <returns><see langword="true"/> when the body states that member.</returns>
    public static bool Stated(UpdatePipelineRequest request, string name) =>
        Members.Single(member => string.Equals(member.Name, name, StringComparison.Ordinal)).Stated(request);
}
