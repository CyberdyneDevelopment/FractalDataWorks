using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>The kinds of answer request a run keeps for its person (FDW-6142): a Question the person answers in words, and a Choice the person answers with one of the options the run showed.</summary>
public interface IAnswerRequestKind : ITypeOption<int, IAnswerRequestKind>
{
    /// <summary>Gets whether the answer must be one of the options the request shows (Choice) and not any text (Question).</summary>
    bool AnswerMustBeAnOption { get; }
}
