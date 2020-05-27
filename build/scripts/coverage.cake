#tool "dotnet:?package=dotnet-reportgenerator-globaltool&version=4.1.5&global"

void GenerateCodeCoverageReport(DirectoryPath testResultsPath, string type){
    var targetDir =  (type == "HTML") ? testResultsPath.Combine(new DirectoryPath("fullReport")) : testResultsPath;

    var args = new ProcessArgumentBuilder ()
        .Append ($"\"-reports:{testResultsPath}/**/*.coverage.xml\"")
        .Append ($"\"-targetdir:{targetDir.FullPath}\"")
        .Append ($"-reporttypes:{type}");

    var settings = new ProcessSettings {
        Arguments = args
    };

    StartProcess ("reportgenerator", settings);
}


List<string> GetExclusions(FilePath testProjectNameWithoutExtension) {
    return new List<string>() {
        // Exclude xunit assemblies from coverage report https://github.com/tonerdo/coverlet/issues/273
        "[xunit.*]*",
        // Don't count execution of test code as covered code
        $"[{testProjectNameWithoutExtension}]*",
    };
}
