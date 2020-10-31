#load "scripts/version.cake"
#load "scripts/publish.cake"
#load "scripts/helpers.cake"

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var packageVersion = string.Empty;
var artifacts = "./dist/";

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
	// Executed BEFORE the first task.
	Information("Running tasks...");
	CreateDirectory(artifacts);
	packageVersion = BuildVersion(fallbackVersion);
});
Setup<BuildData>(ctx => 
{
	Information("Running build setup...");
	var publish = HasEnvironmentVariable("GITHUB_REF") 
        && (EnvironmentVariable("GITHUB_REF").StartsWith("refs/tags/v") || EnvironmentVariable("GITHUB_REF") == "refs/heads/master");
	return new BuildData {
		ReleaseBuild = !string.IsNullOrWhiteSpace(EnvironmentVariable("NUGET_API_KEY")) && publish
	};
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
	Information("Cleaning common files...");
	CleanDirectory(artifacts);
});

Task("NuGet")
	.IsDependentOn("Clean")
	.Does(() => 
{
	CreateDirectory(artifacts + "package");
	Information("Building NuGet package");
	var versionNotes = ParseAllReleaseNotes("./ReleaseNotes.md").FirstOrDefault(v => v.Version.ToString() == packageVersion);
	var content = GetFiles("./scripts/*.cake")
		.Where(f => f.GetExtension() == ".cake")
		.Select(f => new NuSpecContent { Source = f.FullPath, Target = "content"})
		.ToList();
	Information("Packing {0} files", content.Count);
	var settings = new NuGetPackSettings {
		Id				= "DevelopEngine.Cake",
		Version			= packageVersion,
		Title			= "DevelopEngine.Cake",
		Authors		 	= new[] { "Alistair Chapman" },
		Owners			= new[] { "achapman" },
		Description		= "This package contains some reusable Cake scripts used in the DevelopEngine project",
		ReleaseNotes	= versionNotes != null ? versionNotes.Notes.ToList() : new List<string>(),
		Summary			= "Reusable build scripts for DevelopEngine.",
		ProjectUrl		= new Uri("https://github.com/DevelopEngine/DevelopEngine.Cake"),
		IconUrl			= new Uri("https://cdn.rawgit.com/cake-contrib/graphics/a5cf0f881c390650144b2243ae551d5b9f836196/png/cake-contrib-medium.png"),
		License			= new NuSpecLicense { Type = "expression", Value = "MIT" },
		Copyright		= "Alistair Chapman 2020",
		Tags			= new[] { "cake" },
		OutputDirectory = artifacts + "/package",
		Files			= content
		//KeepTemporaryNuSpecFile = true
	};

	NuGetPack(settings);
});

Task("Default")
	.IsDependentOn("Publish-NuGet-Package");

RunTarget(target);
