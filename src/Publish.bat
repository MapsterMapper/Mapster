@echo off
set /p apikey="Enter Nuget.org API key: "
for /r %%v in (Packages\*.nupkg) do nuget push %%v -ApiKey %apikey% -Source nuget.org