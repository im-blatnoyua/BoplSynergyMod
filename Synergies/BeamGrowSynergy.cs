using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Увеличение
    /// Луч работает как обычно (убивает), но дополнительно увеличивает объекты и уменьшает игрока
    /// </summary>
    public static class BeamGrowSynergy
    {
        private static Fix minPlayerScale = (Fix)0.3;

        public static void Activate(SlimeController controller, Player player, int beamIndex, int growIndex)
        {
            Plugin.Log.LogInfo("[BeamGrow] Activating synergy...");

            // Луч активируется стандартным способом и убивает игроков
            // Мы просто добавляем дополнительные эффекты

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item");

            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                // Увеличиваем платформу
                var platform = hit.pp.fixTrans.GetComponent<StickyRoundedRectangle>();
                if (platform != null)
                {
                    Fix growthAmount = (Fix)0.15;
                    var scaleField = Traverse.Create(platform).Field("scale");
                    Fix currentScale = scaleField.GetValue<Fix>();
                    scaleField.SetValue(currentScale + growthAmount);
                    Plugin.Log.LogInfo($"[BeamGrow] Platform grown");
                }
            }

            // Уменьшаем игрока
            if (player.Scale > minPlayerScale)
            {
                Fix scaleReduction = (Fix)0.03;
                player.Scale = Fix.Max(player.Scale - scaleReduction, minPlayerScale);
                Plugin.Log.LogInfo($"[BeamGrow] Player shrunk to {player.Scale}");
            }
        }
    }
}
