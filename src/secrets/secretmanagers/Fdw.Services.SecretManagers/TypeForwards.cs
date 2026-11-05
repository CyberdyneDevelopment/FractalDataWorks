using System.Runtime.CompilerServices;

// Why: these types moved to Fdw.Services.SecretManagers.Abstractions (FDW-3555) so a package that
// only reads a secret needs the abstractions alone. The forwards keep binaries compiled against an
// earlier Fdw.Services.SecretManagers loading.
[assembly: TypeForwardedTo(typeof(Fdw.Services.SecretManagers.SecretValue))]
[assembly: TypeForwardedTo(typeof(Fdw.Services.SecretManagers.Commands.SecretManagerCommandBase))]
[assembly: TypeForwardedTo(typeof(Fdw.Services.SecretManagers.Commands.GetSecretManagerCommand))]
[assembly: TypeForwardedTo(typeof(Fdw.Services.SecretManagers.Logging.GetSecretManagerCommandLog))]
[assembly: TypeForwardedTo(typeof(Fdw.Services.SecretManagers.Logging.SecretManagerCommandBaseLog))]
