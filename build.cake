///////////////////////////////////////////////////////////////////////////////
// Addins
///////////////////////////////////////////////////////////////////////////////

#addin "Cake.Git"

///////////////////////////////////////////////////////////////////////////////
// Arguments
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// Variables
///////////////////////////////////////////////////////////////////////////////

var package = "SonarQube.Scanner.DotNetCore.Tool";

var local = BuildSystem.IsLocalBuild;
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var buildNumber = AppVeyor.Environment.Build.Number;

var branchName = isRunningOnAppVeyor ? EnvironmentVariable("APPVEYOR_REPO_BRANCH") : GitBranchCurrent(DirectoryPath.FromString(".")).FriendlyName;
var isMasterBranch = System.String.Equals("master", branchName, System.StringComparison.OrdinalIgnoreCase);

///////////////////////////////////////////////////////////////////////////////
// Version
///////////////////////////////////////////////////////////////////////////////

var version = "4.3.1";
var toolVersion = "4.3.1.1372";
var semVersion = local ? version : (version + string.Concat("+", buildNumber));

///////////////////////////////////////////////////////////////////////////////
// Tasks
///////////////////////////////////////////////////////////////////////////////

Task("Update-AppVeyor-Build-Number")
    .WithCriteria(() => isRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UpdateBuildVersion(semVersion);
});

Task("Pack")
    .Does(() => {
	    CreateDirectory("nuget");
	    CleanDirectory("nuget");

	    var nuGetPackSettings = new NuGetPackSettings {
                            Id                      = package,
                            Version                 = version,
                            Title                   = package,
                            Authors                 = new[] {"Erik Lichtenstein"},
                            Owners                  = new[] {"Erik Lichtenstein", "cake-contrib"},
                            Description             = "Nuget tool package for SonarQube scanner for .NET Core 2.0",
                            Summary                 = "Contains the SonarCube scanner version " + toolVersion,
                            ProjectUrl              = new Uri("https://github.com/Lichtel/SonarScanner.DotNetCore.Tool"),
                            LicenseUrl              = new Uri("https://github.com/Lichtel/SonarScanner.DotNetCore.Tool/blob/master/LICENSE"),
                            RequireLicenseAcceptance= false,
                            Symbols                 = false,
                            NoPackageAnalysis       = true,
                            Files                   = new [] 
							{
								new NuSpecContent {Source = string.Format(@"**", package), Target = "tools"}
                            },
                            BasePath                = "./scanner",
                            OutputDirectory         = "./nuget"
                        };

    	NuGetPack(nuGetPackSettings);
    });

Task("Publish")
	.IsDependentOn("Pack")
    .WithCriteria(() => isRunningOnAppVeyor)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMasterBranch)
	.Does(() => {		
	    var apiKey = EnvironmentVariable("NUGET_API_KEY");

    	if(string.IsNullOrEmpty(apiKey))    
        	throw new InvalidOperationException("Could not resolve Nuget API key.");
		
		var p = "./nuget/" + package + "." + version + ".nupkg";
            
		// Push the package.
		NuGetPush(p, new NuGetPushSettings {
    		Source = "https://www.nuget.org/api/v2/package",
    		ApiKey = apiKey
		});
	});

Task("AppVeyor")
	.IsDependentOn("Update-AppVeyor-Build-Number")
	.IsDependentOn("Publish");

///////////////////////////////////////////////////////////////////////////////
// Task Targets
///////////////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Pack");

///////////////////////////////////////////////////////////////////////////////
// Execution
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);    
