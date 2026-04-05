using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Увеличение
    /// Луч убивает игроков и постепенно увеличивает объекты, уменьшая стреляющего игрока
    /// </summary>
    public static class BeamGrowSynergy
    {
        private static Fix minPlayerScale = (Fix)0.3; // Минимальный размер игрока

        public static void Activate(SlimeController controller, Player player, int beamIndex, int growIndex)
        {
            Plugin.Log.LogInfo("[BeamGrow] Activating synergy...");

            // Находим компонент Beam
            var beamAbility = FindAbility(controller, beamIndex, growIndex, "beam");
            if (beamAbility == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] Beam ability not found!");
                return;
            }

            // Получаем компонент Beam
            var beam = beamAbility.GetComponent<Beam>();
            if (beam == null)
            {
                Plugin.Log.LogWarning("[BeamGrow] Beam component not found!");
                return;
            }

            // Активируем луч (он убивает игроков)
            beam.OnEnterAbility();

            // Дополнительно применяем эффект увеличения объектов
            ApplyGrowthEffect(controller, player);

            Plugin.Log.LogInfo("[BeamGrow] Synergy activated!");
        }

        private static void ApplyGrowthEffect(SlimeController controller, Player player)
        {
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Делаем raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item");

            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                // Пытаемся найти компонент который можно увеличить
                var platform = hit.pp.fixTrans.GetComponent<StickyRoundedRectangle>();
                if (platform != null)
                {
                    // Увеличиваем платформу
                    Fix growthAmount = (Fix)0.1; // Постепенное увеличение
                    var scaleField = Traverse.Create(platform).Field("scale");
                    Fix currentScale = scaleField.GetValue<Fix>();
                    scaleField.SetValue(currentScale + growthAmount);

                    Plugin.Log.LogInfo($"[BeamGrow] Platform grown to scale {currentScale + growthAmount}");
                }

                // Уменьшаем игрока
                if (player.Scale > minPlayerScale)
                {
                    Fix scaleReduction = (Fix)0.02; // Постепенное уменьшение
                    player.Scale = Fix.Max(player.Scale - scaleReduction, minPlayerScale);
                    Plugin.Log.LogInfo($"[BeamGrow] Player scale reduced to {player.Scale}");
                }
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
