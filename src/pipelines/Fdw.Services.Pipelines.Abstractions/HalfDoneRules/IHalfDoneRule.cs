using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>One thing that may happen to a run found half-done after a restart.</summary>
public interface IHalfDoneRule : ITypeOption<int, IHalfDoneRule>
{
}
