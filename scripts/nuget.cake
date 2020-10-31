Task("NuGet")
    .IsDependentOn("Build")
    .Does<BuildData>(build =>
{
    Information("Building NuGet package");
    CreateDirectory(build.ArtifactsPath + "package/");
    var packSettings = new DotNetCorePackSettings {
        Configuration = build.Configuration,
        NoBuild = true,
        OutputDirectory = $"{build.ArtifactsPath}package",
        ArgumentCustomization = args => args
            .Append($"/p:Version=\"{build.BuildVersion}\"")
            .Append("/p:NoWarn=\"NU1505\"")
    };
    foreach(var project in build.Projects.SourceProjectPaths) {
        Information($"Packing {project.GetDirectoryName()}...");
        DotNetCorePack(project.FullPath, packSettings);
    }
});