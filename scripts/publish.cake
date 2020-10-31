#tool "nuget:https://api.nuget.org/v3/index.json?package=nuget.commandline&version=5.3.1"

Task("Publish-NuGet-Package")
.IsDependentOn("NuGet")
// .IsDependeeOf("Publish")
.WithCriteria<BuildData>((ctx, data) => data.ReleaseBuild)
// .WithCriteria(() => HasEnvironmentVariable("NUGET_TOKEN"))
.Does<BuildData>(build => {
    var nugetToken = EnvironmentVariable("NUGET_TOKEN");
    var pkgFiles = GetFiles($"{build.ArtifactsPath}package/*.nupkg");
	Information($"Pushing {pkgFiles.Count} package files!");
    NuGetPush(pkgFiles, new NuGetPushSettings {
      Source = "https://api.nuget.org/v3/index.json",
      ApiKey = nugetToken
    });
});