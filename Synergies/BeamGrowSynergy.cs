using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Увеличение
    /// Луч увеличивает объекты и одновременно уменьшает игрока
    /// </summary>
    public static class BeamGrowSynergy
    {
        private static Fix minPlayerScale = (Fix)0.3; // Минимальный размер игрока

        public static void Activate(SlimeController controller, Player player, int beamIndex, int growIndex)
        {
            Plugin.Log.LogInfo("[BeamGrow] Activating synergy...");

            // Находим компоненты
            var beamAbility = FindAbilityByName(controller, beamIndex, growIndex, "beam");
            var growAbility = FindAbilityByName(controller, beamIndex, growIndex, "grow");

            if (beamAbility == null || growAbility == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] Required abilities not found!");
                return;
            }

            // Получаем ShootScaleChange компонент
            var scaleGun = growAbility.GetComponent<ShootScaleChange>();
            if (scaleGun == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] ShootScaleChange not found!");
                return;
            }

            // Получаем направление и позицию
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Стреляем лучом увеличения
            bool hasFired = false;
            scaleGun.Shoot(firePos, aimVector, ref hasFired, player.Id);

            // Уменьшаем игрока
            if (player.Scale > minPlayerScale)
            {
                Fix scaleReduction = (Fix)0.05; // Уменьшение за один выстрел
                player.Scale = Fix.Max(player.Scale - scaleReduction, minPlayerScale);
                Plugin.Log.LogInfo($"[BeamGrow] Player scale reduced to {player.Scale}");
            }
            else
            {
                Plugin.Log.LogInfo("[BeamGrow] Player reached minimum scale!");
            }

            // Устанавливаем кулдаун
            SetCooldown(controller, beamIndex, growIndex);
        }

        private static AbilityMonoBehaviour? FindAbilityByName(SlimeController controller, int index1, int index2, string nameContains)
        {
            var ability1 = controller.abilities[index1];
            var ability2 = controller.abilities[index2];

            if (ability1.gameObject.name.ToLower().Contains(nameContains))
                return ability1;
            if (ability2.gameObject.name.ToLower().Contains(nameContains))
                return ability2;

            return null;
        }

        private static void SetCooldown(SlimeController controller, int index1, int index2)
        {
            var cooldownField = Traverse.Create(controller).Field("abilityCooldownTimers");
            var cooldowns = cooldownField.GetValue<Fix[]>();

            if (cooldowns != null)
            {
                cooldowns[index1] = Fix.Zero;
                cooldowns[index2] = Fix.Zero;
            }
        }
    }
}
