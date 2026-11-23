namespace Fdw.Services.Etl.Abstractions.AnswerRequests;

/// <summary>What asking kept (FDW-6142): the request, and whether this call made it or the same step had asked before.</summary>
/// <param name="Request">The request.</param>
/// <param name="IsNew">Whether this call kept the request; false means the step asked before and the request already stood.</param>
public sealed record AnswerRequestKept(AnswerRequest Request, bool IsNew);
