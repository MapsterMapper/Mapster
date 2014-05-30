@echo off


powershell -NoProfile -ExecutionPolicy unrestricted -Command "& {.\archive.ps1; exit $error.Count}"


powershell -NoProfile -ExecutionPolicy unrestricted -Command "& {.\publish.ps1 -PackageName 'Fpr'; exit $error.Count}"


pause