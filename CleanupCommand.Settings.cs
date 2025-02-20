// Copyright (c) Thomas Gossler. All rights reserved.
// Licensed under the MIT license.

#nullable disable warnings

using System.ComponentModel;
using Spectre.Console.Cli;

namespace WebPageHost;

/// <summary>
/// Command line arguments for the cleanup command.
/// </summary>
public sealed class CleanupCommandSettings : CommandSettings
{
    [Description("Name of the user data environment.")]
    [CommandOption("-e|--envname")]
    public string? EnvironmentName { get; init; }
}
