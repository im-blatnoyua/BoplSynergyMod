#!/bin/bash

echo "===== Bopl Synergy Mod - Build & Install ====="
echo ""

# 1. Проверка dotnet
if ! command -v dotnet &> /dev/null; then
    echo "[ERROR] dotnet SDK not found"
    exit 1
fi
echo "[OK] dotnet found"

# 2. Поиск игры
GAME_DIR=""

# Проверяем стандартный путь Steam на Linux
STEAM_PATHS=(
    "$HOME/.steam/steam/steamapps/common/Bopl Battle"
    "$HOME/.local/share/Steam/steamapps/common/Bopl Battle"
)

for path in "${STEAM_PATHS[@]}"; do
    if [ -f "$path/BoplBattle.exe" ]; then
        GAME_DIR="$path"
        break
    fi
done

if [ -z "$GAME_DIR" ]; then
    echo "Game not found automatically."
    read -p "Enter full path to game folder: " GAME_DIR
fi

if [ ! -f "$GAME_DIR/BoplBattle.exe" ]; then
    echo "[ERROR] BoplBattle.exe not found at: $GAME_DIR"
    exit 1
fi

echo "[OK] Game found: $GAME_DIR"

# 3. Проверка BepInEx
BEPINEX_INSTALLED=false
if [ -d "$GAME_DIR/BepInEx/core" ]; then
    echo "[OK] BepInEx found"
    BEPINEX_INSTALLED=true
else
    echo "[WARN] BepInEx not found in game directory"
    echo "       Will copy libs from local cache if available"
fi

# 4. Копирование DLL в libs/
echo "Copying DLLs to libs/..."
cd "$(dirname "$0")"
mkdir -p libs

MANAGED="$GAME_DIR/BoplBattle_Data/Managed"
BEPINEX_CORE="$GAME_DIR/BepInEx/core"

MANAGED_DLLS=(
    "UnityEngine.dll"
    "UnityEngine.CoreModule.dll"
    "UnityEngine.InputLegacyModule.dll"
    "UnityEngine.PhysicsModule.dll"
    "Assembly-CSharp.dll"
    "netstandard.dll"
    "Facepunch.Steamworks.Win64.dll"
)

for dll in "${MANAGED_DLLS[@]}"; do
    if [ -f "$MANAGED/$dll" ]; then
        cp "$MANAGED/$dll" "libs/$dll"
        echo "  Copied: $dll"
    else
        echo "  [WARN] Not found in Managed: $dll"
    fi
done

BEPINEX_DLLS=("BepInEx.dll" "0Harmony.dll")

if [ "$BEPINEX_INSTALLED" = true ]; then
    for dll in "${BEPINEX_DLLS[@]}"; do
        if [ -f "$BEPINEX_CORE/$dll" ]; then
            cp "$BEPINEX_CORE/$dll" "libs/$dll"
            echo "  Copied: $dll"
        else
            echo "  [WARN] Not found in BepInEx/core: $dll"
        fi
    done
else
    echo "  [INFO] Skipping BepInEx DLLs (using existing libs/)"
fi

echo "[OK] libs/ populated"

# 5. Сборка
echo ""
echo "Building..."
dotnet build -c Release
if [ $? -ne 0 ]; then
    echo "[ERROR] Build failed"
    exit 1
fi

# 6. Установка в plugins
if [ "$BEPINEX_INSTALLED" = true ]; then
    mkdir -p "$GAME_DIR/BepInEx/plugins"
    cp "bin/Release/net471/BoplSynergyMod.dll" "$GAME_DIR/BepInEx/plugins/"
    if [ $? -ne 0 ]; then
        echo "[ERROR] Failed to copy DLL to plugins"
        exit 1
    fi
    echo ""
    echo "[OK] Done! Installed to: $GAME_DIR/BepInEx/plugins/BoplSynergyMod.dll"
else
    echo ""
    echo "[OK] Build complete! DLL: bin/Release/net471/BoplSynergyMod.dll"
    echo ""
    echo "To install:"
    echo "  1. Install BepInEx 5.x to game directory"
    echo "  2. Run game once to generate folders"
    echo "  3. Copy BoplSynergyMod.dll to BepInEx/plugins/"
fi
echo ""
