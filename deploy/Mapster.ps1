properties {
	$BaseDir = Resolve-Path "..\"
	$SolutionFile = "$BaseDir\src\Mapster.sln"
	$ProjectPath = "$BaseDir\src\Mapster\Mapster.csproj"	
	$ArchiveDir = "$BaseDir\Deploy\Archive"
	
	$NuGetPackageName = "Mapster"
}

. .\common.ps1
