var publish = Argument<bool>("publish", false);

var shouldPublish = publish && (!string.IsNullOrWhiteSpace(EnvironmentVariable("NUGET_API_KEY")));

Task("Publish")
	.WithCriteria(() => shouldPublish)
	.IsDependentOn("NuGet")
	.Does(() =>
{
	NuGetPush(GetFiles($"{artifacts}package/*.nupkg"), new NuGetPushSettings {
		Source = "https://api.nuget.org/v3/index.json",
		ApiKey = EnvironmentVariable("NUGET_API_KEY")
	});
});