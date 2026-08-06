namespace Fdw.Services.Users.Abstractions;

/// <summary>
/// Configuration options for password policy.
/// </summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>
    /// Gets or sets the name of the hash algorithm for new credentials.
    /// Resolved from PasswordHashAlgorithms TypeCollection.
    /// </summary>
    public string PasswordHashAlgorithm { get; set; } = "Pbkdf2";

    /// <summary>
    /// Gets or sets the maximum age of a password in days before it is considered expired.
    /// Zero or negative means passwords never expire based on age.
    /// </summary>
    public int PasswordMaxAgeDays { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed login attempts that triggers a temporary lockout.
    /// Zero or negative disables lockout (the counter is still tracked but never locks).
    /// </summary>
    // Why: no hardcoded threshold in code — the value comes from policy. Zero/negative = lockout
    // disabled (same "0 = off" convention as PasswordMaxAgeDays), not a fail-loud condition.
    public int MaxFailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets how long (in minutes) an account stays locked after the threshold is reached.
    /// Must be positive when <see cref="MaxFailedLoginAttempts"/> is positive.
    /// </summary>
    public int LockoutDurationMinutes { get; set; }
}
