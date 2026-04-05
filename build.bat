@echo off
dotnet build -c Release
if %errorlevel% neq 0 exit /b %errorlevel%

set BOPL_PATH=%USERPROFILE%\.steam\steam\steamapps\common\Bopl Battle
set PLUGIN_PATH=%BOPL_PATH%\BepInEx\plugins\BoplSynergyMod

if not exist "%PLUGIN_PATH%" mkdir "%PLUGIN_PATH%"
copy /Y "bin\Release\net471\BoplSynergyMod.dll" "%PLUGIN_PATH%\"

echo Build complete and copied to game directory!
