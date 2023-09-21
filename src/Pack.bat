if not exist "Packages" (mkdir "Packages") else (del /F /Q "Packages\*")
dotnet restore Mapster.sln
dotnet msbuild /t:build /p:Configuration=Release /p:GeneratePackageOnBuild=false /p:ExcludeGeneratedDebugSymbol=false Mapster.sln
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -o Packages Mapster.sln