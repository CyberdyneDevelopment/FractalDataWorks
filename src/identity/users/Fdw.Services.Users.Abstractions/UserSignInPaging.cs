using Fdw.Conventions;

namespace Fdw.Services.Users.Abstractions;

/// <summary>
/// The page sizes of an account's sign-in list (FDW-2729, FDW-7103): the take a request that states none gets, and the most a request may ask for.
/// </summary>
/// <remarks>
/// Configuration, never code: the defaults are the users service type option's declared <c>SignInPageDefaultTake</c> and <c>SignInPageMaxTake</c>, a host
/// changes one at registration with <c>ServiceTypeDefaults.Override</c>, and a value stated under <c>Users:Default</c> wins. A request above the most is
/// refused, never cut.
/// </remarks>
public sealed class UserSignInPaging
{
    /// <summary>Initializes a new instance of the <see cref="UserSignInPaging"/> class.</summary>
    /// <param name="defaultTake">The take a request that states none gets.</param>
    /// <param name="maxTake">The most sign-ins one page may hold.</param>
    public UserSignInPaging(int defaultTake, int maxTake)
    {
        DefaultTake = defaultTake;
        MaxTake = maxTake;
    }

    /// <summary>Gets the take a request that states none gets.</summary>
    [PlatformDefault]
    public int DefaultTake { get; }

    /// <summary>Gets the most sign-ins one page may hold.</summary>
    public int MaxTake { get; }
}
