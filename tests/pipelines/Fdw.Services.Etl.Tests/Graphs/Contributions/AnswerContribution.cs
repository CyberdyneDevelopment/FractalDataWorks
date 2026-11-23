using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Tests.Graphs.Contributions;

/// <summary>A third party's own service for its step (FDW-6240), with nothing to inject.</summary>
public sealed class AnswerContribution : IFlowBuildContribution;
