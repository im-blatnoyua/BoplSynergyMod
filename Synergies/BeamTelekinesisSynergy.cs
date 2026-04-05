using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Перемещение объектов
    /// Создаёт эффект притягивания как у синей чёрной дыры
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
                    // Используем формулу притягивания как у чёрной дыры
                    Vec2 direction = controller.body.position - targetBody.position;
                    Fix distance = Vec2.Magnitude(direction);

                    if (distance > (Fix)0.3) // minDistance
                    {
                        Vec2 normalized = direction / distance;
                        Fix G = (Fix)1000L; // Гравитационная константа как у BlackHole
                        Fix force = G / (distance * distance);

                        // Применяем силу
                        targetBody.velocity += normalized * force * (Fix)0.016; // simDeltaTime

                        Plugin.Log.LogInfo("[BeamTelekinesis] Pulling with gravity!");
                        AudioManager.Get()?.Play("fireRaygun");
                    }
                }
            }
        }
    }
}
