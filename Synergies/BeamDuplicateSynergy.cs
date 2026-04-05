using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Размножение
    /// Добавляет сильный recoil эффект к лучу
    /// </summary>
    public static class BeamDuplicateSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int duplicateIndex)
        {
            Plugin.Log.LogInfo("[BeamDuplicate] Activating synergy...");

            // Просто добавляем усиленный recoil эффект
            // Луч активируется стандартным способом через игру
            Vec2 aimVector = player.AimVector();
            Vec2 recoil = aimVector * (Fix)(-15.0); // Очень сильное отталкивание
            controller.body.selfImposedVelocity += recoil;

            Plugin.Log.LogInfo("[BeamDuplicate] Applied strong recoil!");
        }
    }
}
