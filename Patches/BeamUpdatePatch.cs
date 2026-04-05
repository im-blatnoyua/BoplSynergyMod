using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Patches
{
    [HarmonyPatch(typeof(Beam), "UpdateSim")]
    public static class BeamUpdatePatch
    {
        static void Postfix(Beam __instance, Fix SimDeltaTime)
        {
            try
            {
                var ability = Traverse.Create(__instance).Field("ability").GetValue<Ability>();
                if (ability == null) return;

                var playerInfo = ability.GetPlayerInfo();
                var player = PlayerHandler.Get().GetPlayer(playerInfo.playerId);
                if (player == null) return;

                int synergyType = SlimeControllerSynergyPatch.GetActiveSynergy(player.Id);
                if (synergyType == 0) return;

                var body = Traverse.Create(__instance).Field("body").GetValue<PlayerBody>();
                if (body == null) return;

                if (synergyType == 1)
                {
                    ApplyDuplicateBeamEffect(__instance, player, body);
                }
                else if (synergyType == 2)
                {
                    ApplyGrowBeamEffect(__instance, player, body, SimDeltaTime);
                }
                else if (synergyType == 3)
                {
                    ApplyMagnetBeamEffect(__instance, player, body, SimDeltaTime);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[BeamUpdate] Error: {ex.Message}");
            }
        }

        private static void ApplyDuplicateBeamEffect(Beam beam, Player player, PlayerBody body)
        {
            var beamIndex = Traverse.Create(beam).Field("beamIndex").GetValue<int>();
            if (beamIndex < 0) return;

            var timeSinceBeamStart = Traverse.Create(beam).Field("timeSinceBeamStart").GetValue<Fix>();
            var playerBeamColor = Traverse.Create(beam).Field("playerBeamColor").GetValue<DetPhysics.BeamColors>();
            var beamOffset = Traverse.Create(beam).Field("beamOffset").GetValue<Fix>();

            Vec2 aimVector = player.AimVector();
            Vec2 position = body.position + aimVector * beamOffset;
            Fix scale = player.Scale;

            Fix angle45 = (Fix)0.785398;

            Vec2 direction1 = RotateVector(aimVector, angle45);
            Vec2 direction2 = RotateVector(aimVector, -angle45);

            // Используем отрицательные ID чтобы они не убивали владельца
            // (в строке 36528 декомпилированного кода используется Mathf.Abs)
            int beam1Id = -(beam.HierarchyNumber + 1000);
            int beam2Id = -(beam.HierarchyNumber + 2000);

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction1,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam1Id,
                ownerId = player.Id
            });

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction2,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam2Id,
                ownerId = player.Id
            });
        }

        private static Vec2 RotateVector(Vec2 v, Fix angle)
        {
            Fix cos = Fix.Cos(angle);
            Fix sin = Fix.Sin(angle);
            return new Vec2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        private static void ApplyGrowBeamEffect(Beam beam, Player player, PlayerBody body, Fix deltaTime)
        {
            // Проверяем что луч уже активен (не в стадии зарядки)
            var beamIndex = Traverse.Create(beam).Field("beamIndex").GetValue<int>();
            if (beamIndex < 0) return; // Луч ещё заряжается

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = body.position + aimVector * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            // Добавляем "wall" для попадания в острова
            LayerMask collisionMask = LayerMask.GetMask("Default", "item", "wall");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    Fix growthPerSecond = (Fix)0.2;
                    Fix growthThisFrame = growthPerSecond * deltaTime;
                    targetBody.Scale += growthThisFrame;
                }
            }
        }

        private static void ApplyMagnetBeamEffect(Beam beam, Player player, PlayerBody body, Fix deltaTime)
        {
            // Проверяем что луч уже активен (не в стадии зарядки)
            var beamIndex = Traverse.Create(beam).Field("beamIndex").GetValue<int>();
            if (beamIndex < 0) return; // Луч ещё заряжается

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = body.position + aimVector * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            LayerMask mask = LayerMask.GetMask("Default", "item", "wall");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, mask);

            if (hit && hit.pp.fixTrans != null)
            {
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Используем те же значения что и в MagnetGun (строка 19059-19065)
                    Fix pullStr = (Fix)50L; // базовая сила притягивания
                    Fix wallPullStr = (Fix)100L; // для стен/островов

                    // Определяем силу в зависимости от типа объекта
                    Fix force = pullStr;
                    if (hit.pp.fixTrans.gameObject.layer == LayerMask.NameToLayer("wall"))
                    {
                        force = wallPullStr;
                    }

                    // Применяем силу через physicsCollider напрямую (как в строке 7477)
                    Vec2 forceVector = -force * aimVector;
                    var physicsCollider = Traverse.Create(targetBody).Field("physicsCollider").GetValue<IPhysicsCollider>();
                    if (physicsCollider != null)
                    {
                        physicsCollider.AddForce(forceVector);
                    }
                }
            }
        }
    }
}
