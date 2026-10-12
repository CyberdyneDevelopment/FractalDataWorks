using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Pipelines.Loads.Builders;

/// <summary>Empties the destination container.</summary>
[TypeOption(typeof(LoadStepCommandBuilders), "Truncate")]
public sealed class TruncateLoadStepCommandBuilder : LoadStepCommandBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="TruncateLoadStepCommandBuilder"/> class.</summary>
    public TruncateLoadStepCommandBuilder() : base("Truncate")
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyList<IDataCommand>> Build(LoadCommandInput input) => Built(new TruncateCommand());

    /// <inheritdoc/>
    public override IReadOnlyList<string> RequiredCapabilities(PipelineLoadStepConfiguration row) => [];
}
