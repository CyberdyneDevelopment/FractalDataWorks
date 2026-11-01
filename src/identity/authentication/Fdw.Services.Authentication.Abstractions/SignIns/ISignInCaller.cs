namespace Fdw.Services.Authentication.Abstractions.SignIns;

/// <summary>
/// Says who is calling now (FDW-2729), for the log of sign-ins to record beside the sign-in.
/// </summary>
/// <remarks>
/// A sign-in is recorded from many places (the flow runner, the key handler), each deep inside a request that began elsewhere. The caller is read
/// here, once, from the request in hand, so no call site passes it along. A host with no request answers <see cref="SignInCaller.NotKnown"/>.
/// </remarks>
public interface ISignInCaller
{
    /// <summary>Describes the caller of the request in hand.</summary>
    /// <returns>The caller; <see cref="SignInCaller.NotKnown"/> when there is no request.</returns>
    SignInCaller Current();
}
