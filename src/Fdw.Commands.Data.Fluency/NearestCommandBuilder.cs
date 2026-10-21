using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Builds a <see cref="NearestCommand{T}"/> and the target it runs against. Every part is required: the vector field and
/// the vector (<see cref="To"/>), the metric and how many rows; <see cref="Build"/> refuses, naming the part, while one
/// is not given.
/// </summary>
/// <typeparam name="T">The type each row is read as.</typeparam>
public sealed class NearestCommandBuilder<T>
{
    private readonly string _dataStoreName;
    private readonly string _pathName;
    private readonly string _containerName;

    // Each part starts not given, and Build refuses it so: no field, no numbers, no metric, no rows.
    private string _vectorField = string.Empty;
    private IReadOnlyList<float> _vector = [];
    private IVectorMetric _metric = VectorMetrics.NotFound;
    private int _take;

    /// <summary>
    /// Initializes a new instance of the <see cref="NearestCommandBuilder{T}"/> class.
    /// </summary>
    /// <param name="dataStoreName">The data store.</param>
    /// <param name="pathName">The path within the data store.</param>
    /// <param name="containerName">The container whose rows are searched.</param>
    public NearestCommandBuilder(string dataStoreName, string pathName, string containerName)
    {
        _dataStoreName = dataStoreName;
        _pathName = pathName;
        _containerName = containerName;
    }

    /// <summary>
    /// Sets the vector field the distance is measured on, and the vector it is measured from.
    /// </summary>
    /// <param name="vectorField">The container's vector field.</param>
    /// <param name="vector">The vector to measure from: at least one number.</param>
    /// <returns>This builder.</returns>
    public NearestCommandBuilder<T> To(string vectorField, IReadOnlyList<float> vector)
    {
        _vectorField = vectorField;
        _vector = vector;
        return this;
    }

    /// <summary>
    /// Sets how the distance is measured.
    /// </summary>
    /// <param name="metric">A <see cref="VectorMetrics"/> option.</param>
    /// <returns>This builder.</returns>
    public NearestCommandBuilder<T> Metric(IVectorMetric metric)
    {
        _metric = metric;
        return this;
    }

    /// <summary>
    /// Sets how many rows to answer with: the nearest this many.
    /// </summary>
    /// <param name="count">At least one.</param>
    /// <returns>This builder.</returns>
    public NearestCommandBuilder<T> Take(int count)
    {
        _take = count;
        return this;
    }

    /// <summary>
    /// Builds the search, or refuses naming the first part not given or not usable.
    /// </summary>
    /// <returns>The command and its target, or why it could not be built.</returns>
    public IGenericResult<VectorizedDataGatewayCall<IReadOnlyList<NearestResult<T>>>> Build()
    {
        var refusal = Refusal();
        if (refusal is not null)
            return GenericResult<VectorizedDataGatewayCall<IReadOnlyList<NearestResult<T>>>>.Failure(refusal);

        return GenericResult<VectorizedDataGatewayCall<IReadOnlyList<NearestResult<T>>>>.Success(
            new VectorizedDataGatewayCall<IReadOnlyList<NearestResult<T>>>(
                new NearestCommand<T> { VectorField = _vectorField, Vector = _vector, Metric = _metric, Take = _take },
                new DataStoreTarget(_dataStoreName, _pathName, _containerName)));
    }

    // The first part not given, or not usable; none when the search is whole.
    private IGenericMessage? Refusal()
    {
        var logger = NullLogger<NearestCommandBuilder<T>>.Instance;
        foreach (var part in new (string Name, bool Missing)[]
        {
            ("DataStoreName", string.IsNullOrWhiteSpace(_dataStoreName)),
            ("PathName", string.IsNullOrWhiteSpace(_pathName)),
            ("ContainerName", string.IsNullOrWhiteSpace(_containerName)),
            ("VectorField", string.IsNullOrWhiteSpace(_vectorField)),
            ("Metric", _metric == VectorMetrics.NotFound),
        })
        {
            if (part.Missing)
                return NearestCommandLog.RequiredValueMissing(logger, part.Name);
        }

        if (_vector.Count == 0)
            return NearestCommandLog.InvalidInput(logger, "Vector", "a vector has at least one number");

        return _take < 1 ? NearestCommandLog.InvalidInput(logger, "Take", "at least one row is asked for") : null;
    }
}
