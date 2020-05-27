void ExecuteTests(FilePathCollection files, CoverletSettings coverageSettings = null)
{
    var testFailures = false;

    foreach (var file in files)
    {
        var projectName = file.GetFilenameWithoutExtension();

        var testSettings = new DotNetCoreTestSettings
        {
            Configuration = buildConfiguration,
            Logger = $"trx;LogFileName={projectName}.trx",
            NoRestore = true,
            NoBuild = true,
            Verbosity = DotNetCoreVerbosity.Normal,
            ResultsDirectory = testResultsPath
        };

        try
        {
            if (coverageSettings != null)
            {
                coverageSettings.CoverletOutputName = $"{projectName}.coverage.xml";
                coverageSettings.Exclude = GetExclusions(projectName);
                DotNetCoreTest(file.FullPath, testSettings, coverageSettings);
            }
            else
            {
                DotNetCoreTest(file.FullPath, testSettings);
            }
        }
        catch (Exception e)
        {
            Error($"Test project {projectName} has failed tests\n{e}");
            testFailures = true;
        }
    }

    foreach (var file in GetFiles($"{testResultsPath}/**/*.trx"))
    {
        var projectName = file.GetFilenameWithoutExtension();
        Information($"Converting {file} trx result file to junit");
        var transformedFile = testResultsPath.Path.CombineWithFilePath($"{projectName}.junit");
        XmlTransform("./build/trx-to-junit.xsl", file.FullPath, transformedFile.FullPath);
    }

    if (testFailures)
    {
        throw new Exception("One or more test projects has failing tests");
    }
}
