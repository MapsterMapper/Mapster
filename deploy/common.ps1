#Common NuGet/Archiving logic, not meant ot be executed directly.

$framework = '4.5.1'
$currentDate = Get-Date -format yyyyMMddhhmmss

$archiveFolder = "run_$currentDate"

task default -depends Pack

task Init {
        cls
}


task Archive -depends Init {
	New-Item -ItemType Directory -Force -Path archive\$archiveFolder

    if (Test-Path output) {
            Move-Item output\* archive\$archiveFolder
            Remove-Item output\*
    }        
}

task Build -depends Init{
        exec { msbuild $SolutionFile /p:Configuration=Release }
}


task Pack -depends Build {
        exec { nuget pack "$ProjectPath" -OutputDirectory output -Properties Configuration=Release }
}

task Publish -depends Pack {
        $PackageName = gci output\$NuGetPackageName.*.nupkg 
        exec { nuget push $PackageName }
}