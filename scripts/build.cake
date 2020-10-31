var packageVersion = string.Empty;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
	// CreateDirectory(artifacts);
	packageVersion = BuildVersion(fallbackVersion);
	if (FileExists("./build/.dotnet/dotnet.exe")) {
		Information("Using local install of `dotnet` SDK!");
		Context.Tools.RegisterFile("./build/.dotnet/dotnet.exe");
	}
});

Teardown(ctx =>
{
	// Executed AFTER the last task.
	Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
	.Does<BuildData>(data =>
{
	// Clean solution directories.
	foreach(var path in data.Projects.AllProjectPaths)
	{
		Information("Cleaning {0}", path);
		CleanDirectories(path + "/**/bin/" + data.Configuration);
		CleanDirectories(path + "/**/obj/" + data.Configuration);
	}
	Information("Cleaning common files...");
	CreateDirectory(data.ArtifactsPath);
	CleanDirectory(data.ArtifactsPath);
});

Task("Restore")
	.Does<BuildData>(data =>
{
	// Restore all NuGet packages.
	Information("Restoring solution...");
	DotNetCoreRestore(data.Projects.SolutionPath);
});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does<BuildData>(build =>
{
	Information("Building solution...");
	// foreach (var project in projects.SourceProjectPaths) {
	// 	Information($"Building {project.GetDirectoryName()} for {configuration}");
		var settings = new DotNetCoreBuildSettings {
			Configuration = build.Configuration,
			NoIncremental = true,
			ArgumentCustomization = args => args.Append("/p:NoWarn=NU1701").Append($"/p:Version={build.BuildVersion}"),
		};
		DotNetCoreBuild(build.Projects.SolutionPath, settings);
	// }
	
});

Task("Run-Unit-Tests")
	.IsDependentOn("Build")
	.WithCriteria<BuildData>((ctx,build) => build.Projects.TestProjects.Any())
	.Does<BuildData>(build =>
{
	var testResultsPath = MakeAbsolute(Directory(build.ArtifactsPath + "./test-results"));
    CreateDirectory(testResultsPath);
	var settings = new DotNetCoreTestSettings {
		Configuration = build.Configuration
	};

	foreach(var project in build.Projects.TestProjects) {
		DotNetCoreTest(project.Path.FullPath, settings);
	}
});

Task("Post-Build")
	.IsDependentOn("Build")
	.IsDependentOn("Run-Unit-Tests")
	.Does<BuildData>(build =>
{
	CreateDirectory(build.ArtifactsPath + "build");
	foreach (var project in build.Projects.SourceProjects) {
		CreateDirectory(build.ArtifactsPath + "build/" + project.Name);
		foreach (var framework in build.Frameworks) {
			var frameworkDir = $"{build.ArtifactsPath}build/{project.Name}/{framework}";
			CreateDirectory(frameworkDir);
			var files = GetFiles($"{project.Path.GetDirectory()}/bin/{build.Configuration}/{framework}/*.*");
			CopyFiles(files, frameworkDir);
		}
	}
});