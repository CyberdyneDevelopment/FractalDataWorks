using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Provides security token generation, parsing, and validation services for authentication systems.
/// This service handles the low-level token operations that support various authentication mechanisms.
/// </summary>
/// <remarks>
/// <para>
/// The security token service abstracts the complexities of token management across
/// different authentication mechanisms including JWT, API keys, OAuth2 tokens,
/// and custom token formats. It provides a unified interface for token operations
/// while supporting implementation-specific token formats and validation logic.
/// </para>
/// <para>
/// Key responsibilities include:
/// - Generating secure authentication tokens with appropriate claims and metadata
/// - Parsing tokens to extract authentication context information
/// - Validating token integrity, signatures, and expiration
/// - Supporting multiple token formats through pluggable implementations
/// - Ensuring cryptographic security and best practices
/// </para>
/// <para>
/// The service integrates closely with IAuthenticationService and IAuthenticationContext
/// to provide end-to-end token lifecycle management from generation through validation.
/// </para>
/// <para>
/// All operations return IGenericResult to provide consistent error handling and
/// success/failure semantics across the Fdw framework.
/// </para>
/// <para>
/// Implementations should follow security best practices including:
/// - Strong cryptographic signatures for token integrity
/// - Appropriate token expiration times
/// - Secure random number generation for token entropy
/// - Protection against timing attacks in validation
/// - Proper key management and rotation
/// </para>
/// </remarks>
public interface ISecurityTokenService
{
    /// <summary>
    /// Generates a new authentication token based on the provided authentication context.
    /// </summary>
    /// <param name="context">
    /// The authentication context containing user identity, roles, claims, and metadata
    /// to be encoded into the generated token. Cannot be null and must represent
    /// an authenticated user.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the token generation operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous token generation operation.
    /// The task result contains an IGenericResult with the generated token string
    /// if generation succeeds, or error information if generation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Token generation creates a cryptographically secure token that encapsulates
    /// the authentication context information in a format suitable for transmission
    /// and validation. The generated token includes all necessary information for
    /// authentication and authorization decisions.
    /// </para>
    /// <para>
    /// Generation process includes:
    /// - Encoding user identity (UserId, Username) into the token
    /// - Including role assignments and permissions
    /// - Adding relevant claims and custom attributes
    /// - Setting appropriate expiration times
    /// - Applying cryptographic signatures or encryption
    /// - Adding token metadata (issued time, issuer, audience)
    /// </para>
    /// <para>
    /// Token format depends on the authentication method:
    /// - JWT tokens include structured claims with cryptographic signatures
    /// - API keys may be cryptographically secure random strings with database lookups
    /// - OAuth2 tokens follow OAuth2 specification requirements
    /// - Custom tokens use implementation-specific formats
    /// </para>
    /// <para>
    /// The generated token should be suitable for the intended authentication
    /// method and transmission mechanism (HTTP headers, query parameters, etc.).
    /// </para>
    /// <para>
    /// Security considerations:
    /// - Tokens should have appropriate entropy to prevent guessing attacks
    /// - Expiration times should balance security and usability
    /// - Sensitive information should be encrypted if included in tokens
    /// - Token generation should be resistant to timing attacks
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when <paramref name="context"/> represents an unauthenticated user.
    /// </exception>
    Task<IGenericResult<string>> GenerateToken(IAuthenticationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses the provided token and extracts the authentication context information.
    /// </summary>
    /// <param name="token">
    /// The authentication token to parse. Cannot be null or empty.
    /// The token format should match the format generated by this service.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the token parsing operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous token parsing operation.
    /// The task result contains an IGenericResult with the extracted IAuthenticationContext
    /// if parsing succeeds, or error information if parsing fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Token parsing extracts and reconstructs the authentication context that was
    /// originally encoded into the token during generation. This operation is the
    /// inverse of token generation and provides the foundation for authentication
    /// and authorization operations.
    /// </para>
    /// <para>
    /// Parsing process includes:
    /// - Decoding the token structure and format
    /// - Extracting user identity information (UserId, Username)
    /// - Reconstructing role assignments and permissions
    /// - Recovering claims and custom attributes
    /// - Validating token integrity and authenticity
    /// - Checking token expiration and validity periods
    /// </para>
    /// <para>
    /// The parsing operation does not perform full authentication validation
    /// (signature verification, etc.) - it focuses on extracting information
    /// from well-formed tokens. Full validation should be performed using
    /// ValidateTokenAsync for security-sensitive operations.
    /// </para>
    /// <para>
    /// Parsing failures can occur due to:
    /// - Invalid token format or structure
    /// - Corrupted token data
    /// - Unsupported token versions or types
    /// - Missing required token components
    /// - Decryption failures for encrypted tokens
    /// </para>
    /// <para>
    /// The returned authentication context should accurately reflect the
    /// original context used for token generation, allowing for seamless
    /// authentication and authorization processing.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="token"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="token"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult<IAuthenticationContext>> ParseToken(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the provided token for integrity, authenticity, and validity.
    /// </summary>
    /// <param name="token">
    /// The authentication token to validate. Cannot be null or empty.
    /// The token format should match the format generated by this service.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while waiting for the token validation operation to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous token validation operation.
    /// The task result contains an IGenericResult with a boolean value indicating
    /// whether the token is valid (true) or invalid (false).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Token validation performs comprehensive security checks to ensure that
    /// the token is authentic, unmodified, and currently valid for use in
    /// authentication operations. This is a critical security operation that
    /// prevents the use of forged, tampered, or expired tokens.
    /// </para>
    /// <para>
    /// Validation process includes:
    /// - Token format and structure verification
    /// - Cryptographic signature validation (for signed tokens)
    /// - Token expiration and validity period checking
    /// - Issuer and audience validation
    /// - Token revocation status checking
    /// - Nonce and replay attack prevention
    /// - Custom validation rules and policies
    /// </para>
    /// <para>
    /// Validation differs from parsing in that it focuses on security verification
    /// rather than information extraction. A token may parse successfully but
    /// fail validation due to security issues.
    /// </para>
    /// <para>
    /// Common validation failures include:
    /// - Invalid cryptographic signatures
    /// - Expired tokens beyond their validity period
    /// - Tokens from untrusted issuers
    /// - Revoked or blacklisted tokens
    /// - Tokens that have been tampered with or corrupted
    /// - Tokens used outside their intended audience or scope
    /// - Replay attacks using previously valid tokens
    /// </para>
    /// <para>
    /// This method provides lightweight validation suitable for high-frequency
    /// operations where only validity determination is needed, without the
    /// overhead of full context reconstruction.
    /// </para>
    /// <para>
    /// For security-critical applications, validation should be performed
    /// before trusting any token-derived information for authentication
    /// or authorization decisions.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown when <paramref name="token"/> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="token"/> is empty or whitespace.
    /// </exception>
    Task<IGenericResult<bool>> ValidateToken(string token, CancellationToken cancellationToken = default);
}
