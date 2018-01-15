#tool nuget:?package=docfx.console&version=2.18.5
#addin nuget:?package=Cake.DocFx&version=0.5.0

var fallbackVersion = Argument<string>("force-version", EnvironmentVariable("FALLBACK_VERSION") ?? "0.1.0");
var packageVersion = string.Empty;
var testResultsPath = MakeAbsolute(Directory(artifacts + "./test-results"));

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
	CreateDirectory(artifacts);
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
	.Does(() =>
{
	// Clean solution directories.
	foreach(var path in projects.AllProjectPaths)
	{
		Information("Cleaning {0}", path);
		CleanDirectories(path + "/**/bin/" + configuration);
		CleanDirectories(path + "/**/obj/" + configuration);
	}
	Information("Cleaning common files...");
	CleanDirectory(artifacts);
});

Task("Restore")
	.Does(() =>
{
	// Restore all NuGet packages.
	Information("Restoring solution...");
	DotNetCoreRestore(projects.SolutionPath);
});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.Does(() =>
{
	Information("Building solution...");
	foreach (var project in projects.SourceProjectPaths) {
		Information($"Building {project.GetDirectoryName()} for {configuration}");
		var settings = new DotNetCoreBuildSettings {
			Configuration = configuration,
			ArgumentCustomization = args => args.Append("/p:NoWarn=NU1701"),
		};
		DotNetCoreBuild(project.FullPath, settings);
	}
	
});

Task("Run-Unit-Tests")
	.IsDependentOn("Build")
	.Does(() =>
{
    CreateDirectory(testResultsPath);
	if (projects.TestProjects.Any()) {

		var settings = new DotNetCoreTestSettings {
			Configuration = configuration
		};

		foreach(var project in projects.TestProjects) {
			DotNetCoreTest(project.Path.FullPath, settings);
		}
	}
});

Task("Generate-Docs")
	.WithCriteria(() => FileExists("./docfx/docfx.json"))
	.Does(() => 
{
	Information("Building metadata...");
	DocFxMetadata("./docfx/docfx.json");
	Information("Building docs...");
	DocFxBuild("./docfx/docfx.json");
	Information("Packaging built docs...");
	Zip("./docfx/_site/", artifacts + "/docfx.zip");
})
.OnError(ex => 
{
	Warning(ex.Message);
	Warning("Error generating documentation!");
});

Task("Post-Build")
	.IsDependentOn("Build")
	.IsDependentOn("Run-Unit-Tests")
	.IsDependentOn("Generate-Docs")
	.Does(() =>
{
	CreateDirectory(artifacts + "build");
	foreach (var project in projects.SourceProjects) {
		CreateDirectory(artifacts + "build/" + project.Name);
		foreach (var framework in frameworks) {
			var frameworkDir = $"{artifacts}build/{project.Name}/{framework}";
			CreateDirectory(frameworkDir);
			var files = GetFiles($"{project.Path.GetDirectory()}/bin/{configuration}/{framework}/*.*");
			CopyFiles(files, frameworkDir);
		}
	}
});