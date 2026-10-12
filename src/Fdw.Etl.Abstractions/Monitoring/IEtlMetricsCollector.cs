using System;
using System.Collections.Generic;

namespace Fdw.Etl.Abstractions.Monitoring;

/// <summary>
/// Collects metrics for ETL pipeline executions.
/// </summary>
public interface IEtlMetricsCollector
{
    /// <summary>
    /// Records a pipeline start event.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="pipelineId">The pipeline ID.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordPipelineStart(string executionId, string pipelineId, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records a pipeline completion event.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="pipelineId">The pipeline ID.</param>
    /// <param name="success">Whether the execution succeeded.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordPipelineComplete(
        string executionId,
        string pipelineId,
        bool success,
        TimeSpan duration,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records a stage start event.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="stageType">The stage type.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordStageStart(
        string executionId,
        string stageId,
        string stageType,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records a stage completion event.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="stageType">The stage type.</param>
    /// <param name="success">Whether the stage succeeded.</param>
    /// <param name="duration">The stage duration.</param>
    /// <param name="recordsProcessed">Number of records processed.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordStageComplete(
        string executionId,
        string stageId,
        string stageType,
        bool success,
        TimeSpan duration,
        long recordsProcessed,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records a metric value.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordMetric(string name, double value, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records records processed count.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="count">The record count.</param>
    void RecordRecordsProcessed(string executionId, string stageId, long count);

    /// <summary>
    /// Records throughput (records per second).
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="recordsPerSecond">The throughput.</param>
    void RecordThroughput(string executionId, string stageId, double recordsPerSecond);

    /// <summary>
    /// Records data volume (bytes).
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="bytes">The byte count.</param>
    /// <param name="direction">The direction (read/write).</param>
    void RecordDataVolume(string executionId, string stageId, long bytes, string direction);

    /// <summary>
    /// Records an error.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="stageId">The stage ID.</param>
    /// <param name="errorType">The error type.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="properties">Additional properties.</param>
    void RecordError(
        string executionId,
        string stageId,
        string errorType,
        string errorMessage,
        IDictionary<string, string>? properties = null);

    /// <summary>
    /// Records a data quality metric.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="qualityMetricName">The quality metric name.</param>
    /// <param name="value">The metric value.</param>
    void RecordDataQuality(string executionId, string qualityMetricName, double value);

    /// <summary>
    /// Records resource utilization.
    /// </summary>
    /// <param name="executionId">The execution ID.</param>
    /// <param name="cpuPercentage">CPU usage percentage.</param>
    /// <param name="memoryBytes">Memory usage in bytes.</param>
    void RecordResourceUtilization(string executionId, double cpuPercentage, long memoryBytes);
}
