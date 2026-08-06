namespace Fdw.Security.Hashing;

/// <summary>Generates and parses Personal Access Tokens.</summary>
public interface IPersonalAccessTokenGenerator
{
    /// <summary>Generates a new random PAT in format <c>fdx_{environment}_{base62suffix}</c>.</summary>
    string Generate(string environment);

    /// <summary>Returns the first 20 characters of the token for safe display (e.g. <c>fdx_prod_K7mN2pQrXy</c>).</summary>
    string ExtractPrefix(string token);
}
