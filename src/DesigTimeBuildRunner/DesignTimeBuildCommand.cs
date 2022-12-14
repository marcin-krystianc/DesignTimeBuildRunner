using System.Diagnostics;
using Microsoft.Build.Construction;
using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DesigTimeBuildRunner;

public sealed class DesignTimeBuildCommand : AsyncCommand<BuildSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, BuildSettings settings)
    {
        await Task.Yield();

        var dotnetHostPath = DotNetInfoProvider.GetDotNetHostPath();
        var globalProperties = new Dictionary<String, String>
        {
            { "TargetFramework", settings.TargetFramework },
            { "AndroidPreserveUserData", "true" },
            { "AndroidUseManagedDesignTimeResourceGenerator", "True" },
            { "BuildingByReSharper", "True" },
            { "BuildingProject", "False" },
            { "BuildProjectReferences", "False" },
            { "ContinueOnError", "ErrorAndContinue" },
            { "DesignTimeBuild", "True" },
            { "DesignTimeSilentResolution", "False" },
            { "JetBrainsDesignTimeBuild", "True" },
            { "ProvideCommandLineArgs", "True" },
            { "ResolveAssemblyReferencesSilent", "False" },
            { "SkipCompilerExecution", "True" },

            // Necessary to run FSC from non-dotnet processes
            { "FscToolPath", dotnetHostPath.Directory },
            { "FscToolExe", dotnetHostPath.Executeable },
        };

        var targets = settings.Targets.Split(';');

        using var projectCollection = new ProjectCollection();
        var projectOptions = new ProjectOptions
        {
            ProjectCollection = projectCollection,
            LoadSettings = ProjectLoadSettings.IgnoreEmptyImports | ProjectLoadSettings.IgnoreInvalidImports |
                           ProjectLoadSettings.RecordDuplicateButNotCircularImports |
                           ProjectLoadSettings.IgnoreMissingImports,
            EvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared),
            GlobalProperties = globalProperties,
        };

        var projectPaths = new List<string>();

        if (!string.IsNullOrWhiteSpace(settings.Solution))
        {
            var solutionFile = SolutionFile.Parse(settings.Solution);
            var projectsToProcess = solutionFile.ProjectsInOrder
                .Where(x => x.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                .Select(x => x.AbsolutePath);

            projectPaths.AddRange(projectsToProcess);
        }

        if (settings.Projects != null)
        {
            projectPaths.AddRange(settings.Projects);
        }

        var myLogger = new MySimpleLogger();
        var loggers = new List<ILogger> { myLogger };
        if (!string.IsNullOrWhiteSpace(settings.BinLogPath))
        {
            loggers.Add(new BinaryLogger { Parameters = settings.BinLogPath });
        }

        HostServices hostServices = null;
        var buildManager = BuildManager.DefaultBuildManager;
        var submissions = new List<BuildSubmission>();
        var buildParameters = new BuildParameters(projectCollection)
        {
            Loggers = loggers,
            MaxNodeCount = settings.Nodes,
            // MemoryUseLimit = 1,
        };

        var sw = Stopwatch.StartNew();
        buildManager.BeginBuild(buildParameters);
        foreach (var projectPath in projectPaths)
        {
            var project = Project.FromFile(projectPath, projectOptions);

            var projectInstance = buildManager.GetProjectInstanceForBuild(project);
            var targetsForProjectInstance = targets
                .Where(x => projectInstance.Targets.ContainsKey(x))
                .ToArray();

            var buildRequestDataFlags = BuildRequestDataFlags.ProvideProjectStateAfterBuild |
                                        // ReplaceExistingProjectInstance = 1,
                                        // ProvideProjectStateAfterBuild = 2,
                                        // ClearCachesAfterBuild = 8,
                                        // SkipNonexistentTargets = 16
                                        // ProvideSubsetOfStateAfterBuild = 32,
                                        // IgnoreMissingEmptyAndInvalidImports = 64
                                        BuildRequestDataFlags.None;

            var buildSubmission = buildManager.PendBuildRequest(new BuildRequestData(projectInstance,
                targetsForProjectInstance, hostServices, buildRequestDataFlags));

            buildSubmission.ExecuteAsync((submission => HandleSubmission(submission, project, projectInstance)),
                null);
            submissions.Add(buildSubmission);
        }

        foreach (var submission in submissions)
        {
            submission.WaitHandle.WaitOne();
        }

        buildManager.EndBuild();
        var overallSuccess = submissions.All(x => x.BuildResult.OverallResult == BuildResultCode.Success);
        if (overallSuccess)
        {
            AnsiConsole.Markup("[green]Build succeeded![/]\n");
        }
        else
        {
            AnsiConsole.Markup("[red]Build failed![/]\n");
        }

        Console.WriteLine($"Elapsed: {sw.Elapsed}");
        return 0;
    }

    private void HandleSubmission(BuildSubmission submission, Project project,
        ProjectInstance projectInstance)
    {
    }
}