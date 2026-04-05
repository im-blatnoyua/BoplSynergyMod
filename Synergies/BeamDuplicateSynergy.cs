using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Размножение
    /// Создает луч с усиленным recoil эффектом
    /// </summary>
    public static class BeamDuplicateSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int duplicateIndex)
        {
            Plugin.Log.LogInfo("[BeamDuplicate] Activating synergy...");

            // Находим компонент Beam
            var beamAbility = FindBeamAbility(controller, beamIndex, duplicateIndex);
            if (beamAbility == null)
            {
                Plugin.Log.LogWarning("[BeamDuplicate] Beam ability not found!");
                return;
            }

            // Получаем компонент Beam
            var beam = beamAbility.GetComponent<Beam>();
            if (beam == null)
            {
                Plugin.Log.LogWarning("[BeamDuplicate] Beam component not found!");
                return;
            }

            // Активируем луч через его метод
            beam.OnEnterAbility();

            // Применяем усиленный recoil (отталкивание)
            Vec2 aimVector = player.AimVector();
            Vec2 recoil = aimVector * (Fix)(-8.0); // Сильное отталкивание
            controller.body.selfImposedVelocity += recoil;

            Plugin.Log.LogInfo("[BeamDuplicate] Synergy activated with strong recoil!");
        }

        private static AbilityMonoBehaviour FindBeamAbility(SlimeController controller, int index1, int index2)
        {
            var ability1 = controller.abilities[index1];
            var ability2 = controller.abilities[index2];

            if (ability1.gameObject.name.ToLower().Contains("beam"))
                return ability1;
            if (ability2.gameObject.name.ToLower().Contains("beam"))
                return ability2;

            return null;
        }
    }
}
