using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Увеличение
    /// Луч работает как обычно, но создаёт ScaleChanger на объектах и игроке
    /// </summary>
    public static class BeamGrowSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int growIndex)
        {
            Plugin.Log.LogInfo("[BeamGrow] Activating synergy...");

            // Находим способность увеличения
            var growAbility = FindAbility(controller, beamIndex, growIndex, "grow");
            if (growAbility == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] Grow ability not found!");
                return;
            }

            // Получаем ShootScaleChange компонент
            var scaleGun = growAbility.GetComponent<ShootScaleChange>();
            if (scaleGun == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] ShootScaleChange not found!");
                return;
            }

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Стреляем лучом увеличения (создаёт ScaleChanger на объектах)
            bool hasFired = false;
            scaleGun.Shoot(firePos, aimVector, ref hasFired, player.Id);

            // Создаём ScaleChanger на самом игроке для уменьшения
            var scaleChangerPrefab = Traverse.Create(scaleGun).Field("ScaleChangerPrefab").GetValue<ScaleChanger>();
            if (scaleChangerPrefab != null)
            {
                var playerScaler = FixTransform.InstantiateFixed<ScaleChanger>(scaleChangerPrefab, controller.body.position);
                playerScaler.victim = controller.body as IPhysicsCollider;
                playerScaler.player = player;

                // Инвертируем множитель для уменьшения игрока
                playerScaler.multiplier = (Fix)0.7; // Уменьшение до 70%
                playerScaler.PlayerMultiplier = (Fix)0.7;

                Plugin.Log.LogInfo("[BeamGrow] Created scale changers!");
            }
        }

        private static AbilityMonoBehaviour FindAbility(SlimeController controller, int index1, int index2, string nameContains)
        {
            var ability1 = controller.abilities[index1];
            var ability2 = controller.abilities[index2];

            if (ability1.gameObject.name.ToLower().Contains(nameContains))
                return ability1;
            if (ability2.gameObject.name.ToLower().Contains(nameContains))
                return ability2;

            return null;
        }
    }
}
