namespace Fdw.Security.Hashing;

/// <summary>Generates and parses Personal Access Tokens.</summary>
public interface IPersonalAccessTokenGenerator
{
    /// <summary>Generates a new random PAT in format <c>{prefix}{environment}_{base62suffix}</c>.</summary>
    /// <param name="prefix">The credential service's configured prefix (e.g. <c>fdx_</c>), used as given.</param>
    /// <param name="environment">The segment after the prefix: the environment for a PAT, <c>agent</c> for an agent key.</param>
    /// <returns>The raw token.</returns>
    string Generate(string prefix, string environment);

    /// <summary>Returns the first 20 characters of the token for safe display (e.g. <c>fdx_prod_K7mN2pQrXy</c>).</summary>
    string ExtractPrefix(string token);
}
