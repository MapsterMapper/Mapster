$loadedPsake = $false;
if ((Get-Module psake) -eq $null) {
	import-module .\extensions\psake.psm1
	$loadedPsake = $true;
}

invoke-psake "common.ps1" -taskList Archive

if ($loadedPsake) {
	remove-module psake
}