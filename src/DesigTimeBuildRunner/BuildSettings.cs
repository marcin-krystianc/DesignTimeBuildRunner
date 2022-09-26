using System.ComponentModel;
using Spectre.Console.Cli;

namespace DesigTimeBuildRunner;

public class BuildSettings: CommandSettings
{
    [CommandOption("--targets")]
    [DefaultValue("GetSuggestedWorkloads;_CheckForInvalidConfigurationAndPlatform;ResolveReferences;ResolveProjectReferences;ResolveAssemblyReferences;ResolveComReferences;ResolveNativeReferences;ResolveSdkReferences;ResolveFrameworkReferences;ResolvePackageDependenciesDesignTime;Compile;CoreCompile")]
    public string? Targets { get; set; }
    
    [CommandOption("--solution")]
    public string? Solution { get; set; }
    
    [CommandOption("--projects <PROJECTS>")]
    public string[]? Projects { get; set; }
    
    [CommandOption("--binlog")]
    public string? BinLogPath { get; set; }
    
    [CommandOption("--nodes")]
    [DefaultValue(1)]
    public int Nodes { get; set; }
    
    [CommandOption("--tfm")]
    [DefaultValue("net5.0")]
    public string? TargetFramework { get; set; }
}