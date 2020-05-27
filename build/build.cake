#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#addin "nuget:?package=Cake.Docker&version=0.11.0"
#addin "nuget:?package=Cake.Coverlet&version=2.3.4"
#addin "nuget:?package=Cake.Json&version=4.0.0"
#addin "nuget:?package=Newtonsoft.Json&version=12.0.3"
#addin "nuget:?package=Cake.Git&version=0.21.0"
#addin "nuget:?package=Cake.AWS.S3&version=0.6.9&loaddependencies=true"
#tool "nuget:?package=GitVersion.Tool&version=5.1.2"
#l "scripts/coverage.cake"
#l "scripts/utils.cake"

var imageName = Argument<string>("image-name", "codegentest");

// Set current working directory to repository root. That way all relative paths are resolved
// from the project root
System.IO.Directory.SetCurrentDirectory(MakeAbsolute(Directory("../")).FullPath);

var target = Argument("Target", "Build");
var buildConfiguration = Argument<string>("build-config", "Release");
var outputDirectory = Argument<string>("output-directory", "./.artifacts/dist");


var dockerContextPath = Argument<string>("docker-context", ".");
var version = Argument<string>("app-version", ".");
var imageTag = $"{imageName}:{version}";
var dockerContext = Directory(System.IO.Path.GetFullPath(dockerContextPath));
var testResultsPath = Directory(".artifacts/test-results");
var commitSha = Argument<string>("commit-sha", "");
var buildUrl = Argument<string>("build-url", "");

var solutionPath = "codegentest.sln";
var apiPath = "src/codegentest";

Task("GetVersion")
    .Does(() =>
    {
        try{
            var versionInfo = GitVersion(new GitVersionSettings {
                UpdateAssemblyInfo = true,
                OutputType = GitVersionOutput.Json,
				UpdateAssemblyInfoFilePath = "SolutionAssemblyInfo.cs"
            });
            SerializeJsonToFile("build/version.json", versionInfo);
        } catch{
            Information("Cannot generate git version - falling back to original version number");
        }
    });

Task("Build")
    .Description("Builds codegentest.sln.")
    .IsDependentOn("GetVersion")
    .Does(() =>
    {
        var settings = new DotNetCoreBuildSettings
        {
            Configuration = buildConfiguration,
            NoRestore = false,
            Verbosity = DotNetCoreVerbosity.Normal
        };
        DotNetCoreBuild(solutionPath, settings);
    });

Task("IntegrationTests")
    .Description("Runs all xunit integration test projects in ./tests")
    .IsDependentOn("Build")
    .Does(() => ExecuteTests(GetFiles("./tests/**/*.IntegrationTests.csproj")));

Task("UnitTests")
    .Description("Runs all xunit unit test projects in ./tests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var coverageSettings = new CoverletSettings
        {
            CollectCoverage = true,
            CoverletOutputFormat = CoverletOutputFormat.opencover,
            CoverletOutputDirectory = testResultsPath
        };
        ExecuteTests(GetFiles("./tests/**/*.UnitTests.csproj"), coverageSettings);
    });

Task("Tests")
    .IsDependentOn("IntegrationTests")
    .IsDependentOn("UnitTests");

Task("CodeCoverage")
    .Description("Runs Code Coverage Branch Analysis on the coverage.xml reports")
    .IsDependentOn("UnitTests")
    .Does(() =>
    {
        if (!Jenkins.IsRunningOnJenkins) {
            GenerateCodeCoverageReport(testResultsPath, "HTML");
        }
        
        GenerateCodeCoverageReport(testResultsPath, "Cobertura");
    });

Task("Publish")
    .Description("Publish codegentest artifacts to output directory")
    .IsDependentOn("Build")
    .Does(() =>
    {
        var settings = new DotNetCorePublishSettings
        {
            Configuration = buildConfiguration,
            NoRestore = true,
            NoBuild = true,
            Verbosity = DotNetCoreVerbosity.Normal,
            OutputDirectory = outputDirectory
        };

        DotNetCorePublish(apiPath, settings);
    });
	
Task("BuildImage")
    .Description("Builds image from artifacts using Dockerfile")
    .IsDependentOn("Build")
    .IsDependentOn("Publish")
    .Does(() => {
        var settings = new DockerImageBuildSettings
        {
            Tag = new string[] { imageTag },
            BuildArg = new string[] 
            { 
                $"BUILD_DATE={System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")}",
                $"COMMIT_SHA={commitSha}",
                $"BUILD_URL={buildUrl}"
            }
        };
        DockerBuild(settings, dockerContext);
    });


Task("Clean")
    .Description("Cleans created files and images.")
    .Does(() =>
    {
        CleanDirectory(outputDirectory);
        DockerRmi(imageTag);
    })
    .ContinueOnError()
    .ReportError(exception =>
    {
        Error(exception.Message);
        Error(exception.StackTrace);
    });

Task("Ci")
    .Description("Run CI process locally, does not include any deployment related tasks")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("BuildImage");

RunTarget(target);
