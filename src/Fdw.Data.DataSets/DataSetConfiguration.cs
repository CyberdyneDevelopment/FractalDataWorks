using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Schema.Keys;
using Fdw.Schema.Properties;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Configuration class for all dataset types — Standard, Compound, and Federated.
/// Maps to the single <c>data.DataSet</c> table in ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// The DataSet hierarchy has been flattened into a single table.
/// <c>DataSetType</c> on DataSetSource identifies the execution variant (Standard, MultiSource, Distributed).
/// Properties that belong to only one variant are nullable on all rows of other variants.
/// </para>
/// <para>
/// DataSetConfiguration is the root configuration for the DataSet hierarchy:
/// <list type="bullet">
/// <item><description>DataSetConfiguration (parent) - Logical dataset definition</description></item>
/// <item><description>DataSetSourceConfiguration (child entity) - Physical data sources (1:many via DataSetId FK)</description></item>
/// <item><description>DataSetFieldMappingConfiguration (grandchild entity) - Field mappings per source (1:many via DataSetSourceId FK)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataSet")]
public partial class DataSetConfiguration : DomainConfigurationBase, IGenericConfiguration
{






























}
