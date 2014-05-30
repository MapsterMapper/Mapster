param(
	$PackageName
)

$loadedPsake = $false;
if ((Get-Module psake) -eq $null) {
	import-module .\extensions\psake.psm1
	$loadedPsake = $true;
}

invoke-psake "$($PackageName).ps1" -taskList Publish

if ($loadedPsake) {
	remove-module psake
}