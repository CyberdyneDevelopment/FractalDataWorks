using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The root data set a new view varies, and the parent view it hangs from, when it has one.</summary>
/// <param name="Main">The root data set.</param>
/// <param name="Parent">The parent view, or null when the view's parent is the data set itself.</param>
internal sealed record DataSetVariationBase(IDataSetImplementationConfiguration Main, DataSetVariationDomainConfiguration? Parent);
