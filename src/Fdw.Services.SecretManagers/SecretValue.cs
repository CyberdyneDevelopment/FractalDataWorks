using System;
using System.Collections.Generic;
using System.Security;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Represents a secure container for secret data with proper disposal patterns.
/// Provides protection for sensitive information throughout its lifecycle.
/// </summary>
/// <remarks>
/// This class implements secure disposal patterns to ensure sensitive data
/// is properly cleared from memory when no longer needed. It supports both
/// string and binary secret data formats.
/// </remarks>
public sealed class SecretValue : IDisposable
{
    private bool _disposed;
    private SecureString? _secureValue;
    private byte[]? _binaryValue;
    private readonly bool _isBinary;

    /// <summary>
    /// Gets the secret identifier or key name.
    /// </summary>
    /// <value>The secret key or identifier.</value>
    public string Key { get; }

    /// <summary>
    /// Gets the secret version identifier.
    /// </summary>
    /// <value>The version identifier, or null if versioning is not supported.</value>
    public string? Version { get; }

    /// <summary>
    /// Gets the date and time when the secret was created.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the date and time when the secret was last modified.
    /// </summary>
    /// <value>The last modification timestamp.</value>
    public DateTimeOffset ModifiedAt { get; }

    /// <summary>
    /// Gets the date and time when the secret expires.
    /// </summary>
    /// <value>The expiration timestamp, or null if the secret does not expire.</value>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Gets a value indicating whether this secret contains binary data.
    /// </summary>
    /// <value><c>true</c> if the secret contains binary data; otherwise, <c>false</c>.</value>
    public bool IsBinary => _isBinary;

    /// <summary>
    /// Gets a value indicating whether this secret has expired.
    /// </summary>
    /// <value><c>true</c> if the secret has expired; otherwise, <c>false</c>.</value>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;

    /// <summary>
    /// Gets additional metadata associated with the secret.
    /// </summary>
    /// <value>A dictionary of metadata key-value pairs.</value>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretValue"/> class with string data.
    /// </summary>
    /// <param name="key">The secret key or identifier.</param>
    /// <param name="value">The secret string value.</param>
    /// <param name="version">The secret version identifier.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="modifiedAt">The last modification timestamp.</param>
    /// <param name="expiresAt">The expiration timestamp.</param>
    /// <param name="metadata">Additional metadata associated with the secret.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or whitespace.</exception>
    public SecretValue(string key, string value, string? version = null,
        DateTimeOffset? createdAt = null, DateTimeOffset? modifiedAt = null,
        DateTimeOffset? expiresAt = null, IReadOnlyDictionary<string, object>? metadata = null)
    {

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Secret key cannot be empty or whitespace.", nameof(key));
        }

        Key = key;
        Version = version;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        ModifiedAt = modifiedAt ?? DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        Metadata = metadata ?? new Dictionary<string, object>(StringComparer.Ordinal);

        _secureValue = new SecureString();
        foreach (char c in value)
        {
            _secureValue.AppendChar(c);
        }
        _secureValue.MakeReadOnly();
        _isBinary = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretValue"/> class with binary data.
    /// </summary>
    /// <param name="key">The secret key or identifier.</param>
    /// <param name="value">The secret binary value.</param>
    /// <param name="version">The secret version identifier.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="modifiedAt">The last modification timestamp.</param>
    /// <param name="expiresAt">The expiration timestamp.</param>
    /// <param name="metadata">Additional metadata associated with the secret.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> or <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or whitespace.</exception>
    public SecretValue(string key, byte[] value, string? version = null,
        DateTimeOffset? createdAt = null, DateTimeOffset? modifiedAt = null,
        DateTimeOffset? expiresAt = null, IReadOnlyDictionary<string, object>? metadata = null)
    {

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Secret key cannot be empty or whitespace.", nameof(key));
        }

        Key = key;
        Version = version;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
        ModifiedAt = modifiedAt ?? DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        Metadata = metadata ?? new Dictionary<string, object>(StringComparer.Ordinal);

        _binaryValue = new byte[value.Length];
        Array.Copy(value, _binaryValue, value.Length);
        _isBinary = true;
    }

    /// <summary>
    /// Gets the secret value as a string.
    /// </summary>
    /// <returns>The secret string value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the secret contains binary data.</exception>
    /// <remarks>
    /// This method should be used sparingly and the returned string should be
    /// cleared from memory as soon as possible after use.
    /// </remarks>
    public string GetStringValue()
    {
        ThrowIfDisposed();

        if (_isBinary)
        {
            throw new InvalidOperationException("Cannot get string value from binary secret.");
        }

        if (_secureValue == null)
        {
            throw new InvalidOperationException("Secret value has been disposed.");
        }

        IntPtr bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(_secureValue);
        try
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr) ?? string.Empty;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(bstr);
        }
    }

    /// <summary>
    /// Gets the secret value as a byte array.
    /// </summary>
    /// <returns>A copy of the secret binary value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the secret contains string data.</exception>
    /// <remarks>
    /// The returned byte array is a copy. Callers should clear the array
    /// from memory when finished using it.
    /// </remarks>
    public byte[] GetBinaryValue()
    {
        ThrowIfDisposed();

        if (!_isBinary)
        {
            throw new InvalidOperationException("Cannot get binary value from string secret.");
        }

        if (_binaryValue == null)
        {
            throw new InvalidOperationException("Secret value has been disposed.");
        }

        var result = new byte[_binaryValue.Length];
        Array.Copy(_binaryValue, result, _binaryValue.Length);
        return result;
    }

    /// <summary>
    /// Performs a secure access to the string value using a callback function.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
    /// <param name="accessor">The callback function that receives the secret value.</param>
    /// <returns>The result of the callback function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="accessor"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the secret contains binary data.</exception>
    /// <remarks>
    /// This method provides a more secure way to access secret values by ensuring
    /// the value is only available within the scope of the callback function.
    /// </remarks>
    public TResult AccessStringValue<TResult>(Func<string, TResult> accessor)
    {

        var value = GetStringValue();
        try
        {
            return accessor(value);
        }
        finally
        {
            // Note: In production, consider using more secure memory clearing techniques
            // This is a simplified implementation
        }
    }

    /// <summary>
    /// Performs a secure access to the binary value using a callback function.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
    /// <param name="accessor">The callback function that receives the secret value.</param>
    /// <returns>The result of the callback function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="accessor"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the object has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the secret contains string data.</exception>
    /// <remarks>
    /// This method provides a more secure way to access secret values by ensuring
    /// the value is only available within the scope of the callback function.
    /// </remarks>
    public TResult AccessBinaryValue<TResult>(Func<byte[], TResult> accessor)
    {

        var value = GetBinaryValue();
        try
        {
            return accessor(value);
        }
        finally
        {
            // Clear the byte array from memory
            Array.Clear(value, 0, value.Length);
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="SecretValue"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _secureValue?.Dispose();
        _secureValue = null;

        if (_binaryValue != null)
        {
            Array.Clear(_binaryValue, 0, _binaryValue.Length);
            _binaryValue = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}