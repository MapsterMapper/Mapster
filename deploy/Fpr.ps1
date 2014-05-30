properties {
	$BaseDir = Resolve-Path "..\"
	$SolutionFile = "$BaseDir\src\Fpr.sln"
	$ProjectPath = "$BaseDir\src\Fpr\Fpr.csproj"	
	$ArchiveDir = "$BaseDir\Deploy\Archive"
	
	$NuGetPackageName = "Fpr"
}

. .\common.ps1
