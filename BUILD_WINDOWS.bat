@echo off
REM Edit this path to point at your installed RimWorld Managed directory.
set "RIMWORLD_MANAGED=C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed"
dotnet build Source\TarkovCombatInjectors.csproj -c Release -p:RimWorldManaged="%RIMWORLD_MANAGED%"
pause
