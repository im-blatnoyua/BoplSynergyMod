using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Перемещение объектов
    /// Притягивает объекты к игроку
    /// </summary>
    public static class BeamTelekinesisSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int magnetIndex)
        {
            Plugin.Log.LogInfo("[BeamTelekinesis] Activating synergy...");

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item");

            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                Plugin.Log.LogInfo($"[BeamTelekinesis] Hit: {hit.pp.fixTrans.gameObject.name}");

                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Притягиваем к игроку
                    Vec2 pullDirection = Vec2.NormalizedSafe(controller.body.position - targetBody.position);
                    Fix pullStrength = (Fix)15.0;
                    targetBody.velocity += pullDirection * pullStrength;

                    Plugin.Log.LogInfo("[BeamTelekinesis] Pulling object!");
                    AudioManager.Get()?.Play("fireRaygun");
                }
            }
        }
    }
}
