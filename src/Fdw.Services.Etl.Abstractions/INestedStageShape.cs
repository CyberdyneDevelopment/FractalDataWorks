namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// A stage kind that holds stages of its own, and how deep they may nest.
/// </summary>
/// <remarks>
/// The limit is data: it is read from the kind, not written into the validator or the console, and it is listed in
/// <c>/type-collections/TransformTypes/values</c> as <c>maxDepth</c> so a form can stop offering a nested stage at the limit.
/// </remarks>
public interface INestedStageShape
{
    /// <summary>Gets how many Branch stages may be nested inside one another, the outermost counted as one.</summary>
    int MaxDepth { get; }
}
