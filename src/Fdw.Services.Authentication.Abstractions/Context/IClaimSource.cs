using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Where a claim came from, and therefore what it may be used for.</summary>
public interface IClaimSource : ITypeOption<int, ClaimSourceBase>
{
}
