@echo off
REM Requires the .NET SDK and an internet connection for RimRef restore.
REM Reference assemblies are downloaded via NuGet (no RimWorld installation path needed).
dotnet build Source\TarkovCombatInjectors.csproj -c Release
if errorlevel 1 (
  echo Build failed. See compiler messages above.
  exit /b 1
)
echo Built Assemblies\TarkovCombatInjectors.dll
pause
