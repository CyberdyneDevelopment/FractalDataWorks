using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Data.DataStores.Abstractions;

/// <summary>
/// Represents a complete data location descriptor that combines store and path information.
/// This class provides a unified way to specify exactly where data can be found.
/// </summary>
/// <remarks>
/// DataLocation serves as a complete address for data within the universal access system.
/// It combines the WHERE (data store) with the HOW (data path) to create a full
/// specification that can be used by the system to locate and access data.
/// </remarks>
public sealed class DataLocation : IEquatable<DataLocation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataLocation"/> class.
    /// </summary>
    /// <param name="storeId">The identifier of the data store.</param>
    /// <param name="pathId">The identifier of the data path within the store.</param>
    /// <param name="containerType">The type of data container at this location.</param>
    /// <param name="parameters">Optional parameters for parameterized paths.</param>
    /// <param name="metadata">Optional metadata about this location.</param>
    public DataLocation(
        string storeId,
        string pathId,
        string containerType,
        IDictionary<string, object>? parameters = null,
        IDictionary<string, object>? metadata = null)
    {
        StoreId = storeId ?? throw new ArgumentNullException(nameof(storeId));
        PathId = pathId ?? throw new ArgumentNullException(nameof(pathId));
        ContainerType = containerType ?? throw new ArgumentNullException(nameof(containerType));

        Parameters = parameters != null
            ? new Dictionary<string, object>(parameters, StringComparer.Ordinal)
            : new Dictionary<string, object>(StringComparer.Ordinal);

        Metadata = metadata != null
            ? new Dictionary<string, object>(metadata, StringComparer.Ordinal)
            : new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the identifier of the data store.
    /// </summary>
    /// <value>The unique identifier for the store where data is located.</value>
    public string StoreId { get; }

    /// <summary>
    /// Gets the identifier of the data path within the store.
    /// </summary>
    /// <value>The unique identifier for the path within the specified store.</value>
    public string PathId { get; }

    /// <summary>
    /// Gets the type of data container at this location.
    /// </summary>
    /// <value>
    /// The container type identifier (e.g., "SqlTable", "JsonFile", "RestEndpoint").
    /// </value>
    public string ContainerType { get; }

    /// <summary>
    /// Gets the parameters for parameterized paths.
    /// </summary>
    /// <value>
    /// Key-value pairs for substituting parameters in path templates.
    /// For example, {"customerId": 123} for a path like "/customers/{customerId}".
    /// </value>
    public IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Gets metadata about this location.
    /// </summary>
    /// <value>Additional properties and configuration information.</value>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this location has parameters that need resolution.
    /// </summary>
    /// <value>
    /// <c>true</c> if this location contains parameter values; otherwise, <c>false</c>.
    /// </value>
    public bool HasParameters => Parameters.Count > 0;

    /// <summary>
    /// Creates a new location with additional or updated parameters.
    /// </summary>
    /// <param name="additionalParameters">The parameters to add or update.</param>
    /// <returns>A new DataLocation instance with the combined parameters.</returns>
    /// <remarks>
    /// This method provides a way to extend locations with runtime parameter values.
    /// Existing parameters are preserved unless overridden by the additional parameters.
    /// </remarks>
    public DataLocation WithParameters(IDictionary<string, object> additionalParameters)
    {
        if (additionalParameters == null || additionalParameters.Count == 0)
            return this;

        var combinedParameters = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Parameters)
        {
            combinedParameters[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in additionalParameters)
        {
            combinedParameters[kvp.Key] = kvp.Value;
        }

        var metadataDict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Metadata)
        {
            metadataDict[kvp.Key] = kvp.Value;
        }
        return new DataLocation(StoreId, PathId, ContainerType, combinedParameters, metadataDict);
    }

    /// <summary>
    /// Creates a new location with additional or updated metadata.
    /// </summary>
    /// <param name="additionalMetadata">The metadata to add or update.</param>
    /// <returns>A new DataLocation instance with the combined metadata.</returns>
    /// <remarks>
    /// This method provides a way to extend locations with additional metadata.
    /// Existing metadata is preserved unless overridden by the additional metadata.
    /// </remarks>
    public DataLocation WithMetadata(IDictionary<string, object> additionalMetadata)
    {
        if (additionalMetadata == null || additionalMetadata.Count == 0)
            return this;

        var combinedMetadata = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Metadata)
        {
            combinedMetadata[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in additionalMetadata)
        {
            combinedMetadata[kvp.Key] = kvp.Value;
        }

        var parametersDict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Parameters)
        {
            parametersDict[kvp.Key] = kvp.Value;
        }
        return new DataLocation(StoreId, PathId, ContainerType, parametersDict, combinedMetadata);
    }

    /// <summary>
    /// Creates a new location with a different container type.
    /// </summary>
    /// <param name="newContainerType">The new container type.</param>
    /// <returns>A new DataLocation instance with the updated container type.</returns>
    /// <remarks>
    /// This method is useful when the same data can be accessed in different formats
    /// from the same store and path. For example, an API endpoint might support
    /// both JSON and XML responses.
    /// </remarks>
    public DataLocation WithContainerType(string newContainerType)
    {
        if (string.IsNullOrWhiteSpace(newContainerType))
            throw new ArgumentException("Container type cannot be null or empty", nameof(newContainerType));

        var parametersDict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Parameters)
        {
            parametersDict[kvp.Key] = kvp.Value;
        }
        var metadataDict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kvp in Metadata)
        {
            metadataDict[kvp.Key] = kvp.Value;
        }
        return new DataLocation(StoreId, PathId, newContainerType, parametersDict, metadataDict);
    }

    /// <summary>
    /// Creates a canonical string representation of this location.
    /// </summary>
    /// <returns>
    /// A string representation in the format "store://path@container?parameters".
    /// </returns>
    /// <remarks>
    /// The canonical format provides a consistent way to represent data locations
    /// as strings for logging, configuration, and debugging purposes. The format is:
    /// - storeId://pathId@containerType
    /// - Parameters are appended as query string if present
    /// - Example: "salesdb://orders@SqlTable?customerId=123&amp;year=2024"
    /// </remarks>
    public string ToCanonicalString()
    {
        var result = $"{StoreId}://{PathId}@{ContainerType}";

        if (HasParameters)
        {
            var parameterPairs = new List<string>();
            foreach (var kvp in Parameters)
            {
                parameterPairs.Add($"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}");
            }
            result += "?" + string.Join("&", parameterPairs);
        }

        return result;
    }

    /// <summary>
    /// Parses a canonical string representation back into a DataLocation.
    /// </summary>
    /// <param name="canonicalString">The canonical string to parse.</param>
    /// <returns>
    /// A success result carrying the parsed <see cref="DataLocation"/>, or a fail-loud
    /// <see cref="InvalidCanonicalLocationFormatCode"/> failure if the format is malformed.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="canonicalString"/> is null/empty/whitespace — a programmer
    /// error (missing required argument), distinct from a malformed-but-present value.
    /// </exception>
    /// <remarks>
    /// This method is the inverse of ToCanonicalString, allowing DataLocation
    /// instances to be reconstructed from string representations.
    /// </remarks>
    public static IGenericResult<DataLocation> Parse(string canonicalString)
    {
        if (string.IsNullOrWhiteSpace(canonicalString))
            throw new ArgumentException("Canonical string cannot be null or empty", nameof(canonicalString));

        // Split on '?' to separate path from parameters
        var parts = canonicalString.Split(['?'], 2);
        var pathPart = parts[0];
        var parameterPart = parts.Length > 1 ? parts[1] : null;

        // Parse the path part: storeId://pathId@containerType
        var pathSegments = pathPart.Split(["://"], 2, StringSplitOptions.None);
        if (pathSegments.Length != 2)
            return GenericResult<DataLocation>.Failure(
                DataStoresResultCodes.ByName("InvalidCanonicalLocationFormat"),
                ResultDetails.Create("Input", canonicalString, "Reason", "Missing '://' separator"));

        var storeId = pathSegments[0];
        var pathAndContainer = pathSegments[1].Split(['@'], 2);
        if (pathAndContainer.Length != 2)
            return GenericResult<DataLocation>.Failure(
                DataStoresResultCodes.ByName("InvalidCanonicalLocationFormat"),
                ResultDetails.Create("Input", canonicalString, "Reason", "Missing '@' separator"));

        var pathId = pathAndContainer[0];
        var containerType = pathAndContainer[1];

        // Parse parameters if present
        var parameters = new Dictionary<string, object>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(parameterPart))
        {
            var paramPairs = parameterPart!.Split(['&']);
            foreach (var pair in paramPairs)
            {
                var keyValue = pair.Split(['='], 2);
                if (keyValue.Length == 2)
                {
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = Uri.UnescapeDataString(keyValue[1]);
                    parameters[key] = value;
                }
            }
        }

        return GenericResult<DataLocation>.Success(new DataLocation(storeId, pathId, containerType, parameters));
    }

    #region IEquatable<DataLocation> Implementation

    /// <inheritdoc/>
    public bool Equals(DataLocation? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(StoreId, other.StoreId, StringComparison.Ordinal)
            && string.Equals(PathId, other.PathId, StringComparison.Ordinal)
            && string.Equals(ContainerType, other.ContainerType, StringComparison.Ordinal)
            && ParametersEqual(Parameters, other.Parameters);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as DataLocation);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(StoreId);
        hash.Add(PathId);
        hash.Add(ContainerType);

        // Include parameter keys and values in hash
        foreach (var kvp in Parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two DataLocation instances are equal.
    /// </summary>
    /// <param name="left">The first location to compare.</param>
    /// <param name="right">The second location to compare.</param>
    /// <returns><c>true</c> if the locations are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(DataLocation? left, DataLocation? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Determines whether two DataLocation instances are not equal.
    /// </summary>
    /// <param name="left">The first location to compare.</param>
    /// <param name="right">The second location to compare.</param>
    /// <returns><c>true</c> if the locations are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(DataLocation? left, DataLocation? right)
    {
        return !Equals(left, right);
    }

    private static bool ParametersEqual(IReadOnlyDictionary<string, object> left, IReadOnlyDictionary<string, object> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out var rightValue))
                return false;

            if (!Equals(kvp.Value, rightValue))
                return false;
        }

        return true;
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
        return ToCanonicalString();
    }
}