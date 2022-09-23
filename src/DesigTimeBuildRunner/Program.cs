﻿
using Microsoft.Build.Locator;
using Spectre.Console.Cli;

namespace DesigTimeBuildRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var registeredInstance = MSBuildLocator.RegisterDefaults();
            var app = new CommandApp<DesignTimeBuildCommand>();
            await app.RunAsync(args);
        }
    }
}