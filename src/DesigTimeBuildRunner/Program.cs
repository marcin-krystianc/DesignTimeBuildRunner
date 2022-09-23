// See https://aka.ms/new-console-template for more information

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace DesigTimeBuildRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var registeredInstance = MSBuildLocator.RegisterDefaults();
            DoWork();
        }

        static void DoWork()
        {
            var globalProperties = new Dictionary<String, String>
            {
                //{"RestoreDisableParallel", "true"},
                //{"RestoreForce", "true"},
                //{"RestoreUseStaticGraphEvaluation", "true"}, // new process
                //{"EnableTransitiveDependencyPinning", "false"},
                { "TargetFramework", "net5.0" },
            };

            var target = "Restore";

            var targets = new[]
            {
                "GetSuggestedWorkloads", "_CheckForInvalidConfigurationAndPlatform", "ResolveReferences",
                "ResolveProjectReferences", "ResolveAssemblyReferences", "ResolveComReferences",
                "ResolveNativeReferences", "ResolveSdkReferences", "ResolveFrameworkReferences",
                "ResolvePackageDependenciesDesignTime", "Compile", "CoreCompile"
            };

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
            var projectPaths = new[]
            {
                @"D:\workspace\TestSolutions\SimpleAppWithLibs\App\App.csproj",
                @"D:\workspace\TestSolutions\SimpleAppWithLibs\Lib1\Lib1.csproj",
                @"D:\workspace\TestSolutions\SimpleAppWithLibs\Lib2\Lib2.csproj",
            };

            HostServices hostServices = null;
            var buildManager = BuildManager.DefaultBuildManager;
            var submisions = new List<BuildSubmission>();
            var myLogger = new MySimpleLogger();
            var buildParameters = new BuildParameters(projectCollection)
            {
                Loggers = new ILogger[]
                {
                    myLogger,
                    new BinaryLogger{Parameters = "build.binlog"}
                },
            };

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
                submisions.Add(buildSubmission);
            }

            foreach (var submission in submisions)
            {
                submission.WaitHandle.WaitOne();
            }

            buildManager.EndBuild();
            Console.WriteLine("Build complete!");
        }

        private static void HandleSubmission(BuildSubmission submission, Project project,
            ProjectInstance projectInstance)
        {
        }
    }
}