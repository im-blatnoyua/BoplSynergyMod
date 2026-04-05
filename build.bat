@echo off
setlocal enabledelayedexpansion

echo ===== Bopl Synergy Mod - Build =====
echo.

:: 1. Check dotnet
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] dotnet SDK not found.
    pause
    exit /b 1
)
echo [OK] dotnet found

:: 2. Find game folder
set "GAME_DIR="

for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v "InstallPath" 2^>nul') do set "STEAM_REG=%%b"

if defined STEAM_REG (
    if exist "!STEAM_REG!\steamapps\common\Bopl Battle\BoplBattle.exe" (
        set "GAME_DIR=!STEAM_REG!\steamapps\common\Bopl Battle"
    )
)

if not defined GAME_DIR (
    echo [WARN] Game not found automatically.
    echo.
)

:: 3. Copy DLLs to libs/ if game found
if defined GAME_DIR (
    echo [OK] Game found: !GAME_DIR!

    if exist "!GAME_DIR!\BepInEx\core" (
        echo [OK] BepInEx found

        echo Copying DLLs to libs/...
        cd /d "%~dp0"
        if not exist "libs" mkdir libs

        set "MANAGED=!GAME_DIR!\BoplBattle_Data\Managed"
        set "BEPINEX_CORE=!GAME_DIR!\BepInEx\core"

        for %%F in (
            UnityEngine.dll
            UnityEngine.CoreModule.dll
            UnityEngine.InputLegacyModule.dll
            UnityEngine.PhysicsModule.dll
            Assembly-CSharp.dll
            Facepunch.Steamworks.Win64.dll
            netstandard.dll
        ) do (
            if exist "!MANAGED!\%%F" (
                copy /y "!MANAGED!\%%F" "libs\%%F" >nul
            )
        )

        for %%F in (BepInEx.dll 0Harmony.dll) do (
            if exist "!BEPINEX_CORE!\%%F" (
                copy /y "!BEPINEX_CORE!\%%F" "libs\%%F" >nul
            )
        )

        echo [OK] libs/ populated
    )
)

:: 4. Build
echo.
echo Building mod...
dotnet build -c Release
if errorlevel 1 (
    echo [ERROR] Build failed
    pause
    exit /b 1
)

:: 5. Copy to local output folder
cd /d "%~dp0"
if not exist "output" mkdir output

echo.
echo Copying DLL to output folder...

:: Проверяем оба возможных пути
set "DLL_PATH="
if exist "bin\Release\net471\BoplSynergyMod.dll" (
    set "DLL_PATH=bin\Release\net471\BoplSynergyMod.dll"
)
if exist "obj\Release\net471\BoplSynergyMod.dll" (
    set "DLL_PATH=obj\Release\net471\BoplSynergyMod.dll"
)

if not defined DLL_PATH (
    echo [ERROR] DLL not found in bin or obj folders
    pause
    exit /b 1
)

echo Found DLL at: !DLL_PATH!
copy /y "!DLL_PATH!" "output\BoplSynergyMod.dll"
if errorlevel 1 (
    echo [ERROR] Failed to copy to output folder
    pause
    exit /b 1
)

if not exist "output\BoplSynergyMod.dll" (
    echo [ERROR] DLL not found in output folder after copy
    pause
    exit /b 1
)

echo [OK] DLL copied successfully
echo.
echo File location: %~dp0output\BoplSynergyMod.dll
echo File size:
dir "output\BoplSynergyMod.dll" | find "BoplSynergyMod.dll"
echo.
echo To install:
if defined GAME_DIR (
    echo   Copy to: !GAME_DIR!\BepInEx\plugins\
) else (
    echo   Copy to: [Game folder]\BepInEx\plugins\
)
echo.
pause
