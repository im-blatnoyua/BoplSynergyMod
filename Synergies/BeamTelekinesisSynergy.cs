using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Перемещение объектов
    /// Луч притягивает объекты к игроку
    /// </summary>
    public static class BeamTelekinesisSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int magnetIndex)
        {
            Plugin.Log.LogInfo("[BeamTelekinesis] Activating synergy...");

            // Получаем направление и позицию
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Делаем raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item");

            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                Plugin.Log.LogInfo($"[BeamTelekinesis] Hit object: {hit.pp.fixTrans.gameObject.name}");

                // Получаем BoplBody объекта
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Вычисляем направление притягивания (от объекта к игроку)
                    Vec2 pullDirection = Vec2.NormalizedSafe(controller.body.position - targetBody.position);

                    // Применяем силу притягивания
                    Fix pullStrength = (Fix)10.0; // Увеличенная сила притягивания
                    targetBody.velocity += pullDirection * pullStrength;

                    Plugin.Log.LogInfo($"[BeamTelekinesis] Pulling object with force {pullStrength}");

                    // Визуальный эффект
                    AudioManager.Get()?.Play("fireRaygun");
                }
                else
                {
                    Plugin.Log.LogInfo("[BeamTelekinesis] Target has no BoplBody");
                }
            }
            else
            {
                Plugin.Log.LogInfo("[BeamTelekinesis] No target hit");
            }
        }
    }
}
