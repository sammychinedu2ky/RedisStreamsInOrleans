using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Pack);

    private const string NuGetSourceUrl = "https://api.nuget.org/v3/index.json";
    private const string LibraryProjectName = "Universley.OrleansContrib.StreamsProvider.Redis";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [GitVersion(NoFetch = true)] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory;
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    AbsolutePath NuGetPackagesDirectory => ArtifactsDirectory / "nuget";
    AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";

    // Nuget API key can be also set as an environment variable
    [Parameter("NuGet API key")]
    readonly string NuGetApiKey;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(ap => Directory.Delete(ap, true));
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var semVer = GitVersion.AssemblySemVer;
            var semFileVer = GitVersion.AssemblySemFileVer;
            var informationalVersion = GitVersion.InformationalVersion;

            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(semVer)
                .SetFileVersion(semFileVer)
                .SetInformationalVersion(informationalVersion)
                .EnableNoRestore());
        });

    Target Test => _ => _
       .DependsOn(Compile)
       .Executes(() =>
       {
           DotNetTasks.DotNetTest(s => s
                   .SetProjectFile(Solution)
                   .SetConfiguration(Configuration)
                   .SetLoggers($"trx;LogFileName={TestResultDirectory / "testresults.trx"}")
           );
       });

    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {

            DotNetTasks.DotNetPack(s => s
                    .SetProject(SourceDirectory / "Provider" / $"{LibraryProjectName}.csproj")
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(NuGetPackagesDirectory)
                    .SetVersion(GitVersion.NuGetVersionV2)
                    .SetNoBuild(true)
                 );
        });

    Target Publish => _ => _
            .DependsOn(Pack)
            .Requires(() => !string.IsNullOrEmpty(NuGetApiKey))
            .Executes(() =>
            {
                DotNetTasks.DotNetNuGetPush(s => s
                    .SetSource(NuGetSourceUrl)
                    .SetApiKey(NuGetApiKey)
                    .SetTargetPath(NuGetPackagesDirectory / $"{LibraryProjectName}.{GitVersion.NuGetVersionV2}.snupkg"));

                DotNetTasks.DotNetNuGetPush(s => s
                     .SetSource(NuGetSourceUrl)
                     .SetApiKey(NuGetApiKey)
                     .SetTargetPath(NuGetPackagesDirectory / $"{LibraryProjectName}.{GitVersion.NuGetVersionV2}.nupkg"));
            });

}
