var publish = Argument<bool>("publish", false);

var shouldPublish = publish && (!string.IsNullOrWhiteSpace(EnvironmentVariable("NUGET_API_KEY")));