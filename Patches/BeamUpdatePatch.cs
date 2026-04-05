using BoplFixedMath;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BoplSynergyMod.Patches
{
    [HarmonyPatch(typeof(Beam), "UpdateSim")]
    public static class BeamUpdatePatch
    {
        // Отслеживаем для каких лучей уже создали дополнительные лучи
        private static HashSet<int> processedBeams = new HashSet<int>();

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
                if (synergyType != 0)
                {
                    Plugin.Log.LogInfo($"[BeamUpdate] Player {player.Id} has synergy type {synergyType}");
                }
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
                else if (synergyType == 4)
                {
                    ApplyShrinkBeamEffect(__instance, player, body, SimDeltaTime);
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

            // Проверяем что мы ещё не создавали дополнительные лучи для этого луча
            int beamInstanceId = beam.GetInstanceID();
            if (processedBeams.Contains(beamInstanceId))
            {
                return; // Уже создали лучи для этого экземпляра
            }
            processedBeams.Add(beamInstanceId);

            var timeSinceBeamStart = Traverse.Create(beam).Field("timeSinceBeamStart").GetValue<Fix>();
            var playerBeamColor = Traverse.Create(beam).Field("playerBeamColor").GetValue<DetPhysics.BeamColors>();
            var beamOffset = Traverse.Create(beam).Field("beamOffset").GetValue<Fix>();
            var currentGround = Traverse.Create(beam).Field("currentGround").GetValue<StickyRoundedRectangle>();
            var staffDir = Traverse.Create(beam).Field("staffDir").GetValue<Vec2>();

            Vec2 position = body.position + staffDir * beamOffset;
            Fix scale = body.fixtrans.Scale;

            // Проверяем уровень воды (как в строке 627)
            if (position.y < Constants.WATER_HEIGHT && Constants.leveltype != LevelType.space)
            {
                return; // Под водой лучи не работают
            }

            Fix angle45 = (Fix)0.785398;

            Vec2 direction1 = RotateVector(staffDir, angle45);
            Vec2 direction2 = RotateVector(staffDir, -angle45);

            // Смещаем лучи в стороны чтобы они не пересекались в центре
            // Вычисляем перпендикуляр к направлению луча
            Vec2 perpendicular = new Vec2(-staffDir.y, staffDir.x);
            Fix sideOffset = (Fix)3.0; // Смещение в стороны

            Vec2 position1 = position + perpendicular * sideOffset;
            Vec2 position2 = position - perpendicular * sideOffset;

            // Используем уникальные отрицательные ID для каждого луча
            int beam1Id = -(1000000 + player.Id * 1000 + 1);
            int beam2Id = -(1000000 + player.Id * 1000 + 2);

            // ВАЖНО: Используем ownerId = -1 (нейтральный) чтобы лучи не убивали никого
            // Это сделает их безопасными для всех игроков
            int neutralOwnerId = -1;

            Plugin.Log.LogInfo($"[TripleBeam] Creating offset beams: pos1=({position1.x},{position1.y}), pos2=({position2.x},{position2.y})");

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position1, // Смещённая позиция
                direction = direction1,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam1Id,
                ownerId = neutralOwnerId,
                ground = currentGround
            });

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position2, // Смещённая позиция
                direction = direction2,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam2Id,
                ownerId = neutralOwnerId,
                ground = currentGround
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

            var staffDir = Traverse.Create(beam).Field("staffDir").GetValue<Vec2>();
            Vec2 firePos = body.position + staffDir * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            // Добавляем "wall" для попадания в острова
            LayerMask collisionMask = LayerMask.GetMask("Default", "item", "wall");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, staffDir, maxDistance, collisionMask);

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
            if (beamIndex < 0)
            {
                Plugin.Log.LogInfo($"[Magnet] Beam not active yet (beamIndex={beamIndex})");
                return;
            }

            var staffDir = Traverse.Create(beam).Field("staffDir").GetValue<Vec2>();
            Vec2 firePos = body.position + staffDir * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            LayerMask mask = LayerMask.GetMask("Default", "item", "wall", "Projectile");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, staffDir, maxDistance, mask);

            Plugin.Log.LogInfo($"[Magnet] Raycast from ({firePos.x},{firePos.y}) dir ({staffDir.x},{staffDir.y}), hit={hit != null}");

            if (hit && hit.pp.fixTrans != null)
            {
                Plugin.Log.LogInfo($"[Magnet] Hit object: {hit.pp.fixTrans.gameObject.name}, layer={hit.pp.fixTrans.gameObject.layer}");

                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Используем значения из MagnetGun.TryPullItems (строка 19449-19480)
                    Fix pullStr = (Fix)50L;
                    Fix projectilePullStr = (Fix)50L;
                    Fix wallPullStr = (Fix)100L;

                    Fix force = pullStr;
                    int layer = hit.pp.fixTrans.gameObject.layer;

                    if (layer == LayerMask.NameToLayer("Projectile"))
                    {
                        force = projectilePullStr;
                    }
                    else if (layer == LayerMask.NameToLayer("wall"))
                    {
                        force = wallPullStr;
                    }

                    // Направление притягивания: от объекта К игроку (отрицательное направление луча)
                    Vec2 pullDirection = -staffDir;
                    Vec2 forceVector = force * pullDirection;

                    // Применяем силу через physicsCollider напрямую (обходим ForceMode2D)
                    var physicsCollider = Traverse.Create(targetBody).Field("physicsCollider").GetValue<IPhysicsCollider>();
                    if (physicsCollider != null)
                    {
                        physicsCollider.AddForce(forceVector);
                        Plugin.Log.LogInfo($"[Magnet] Applied force ({forceVector.x},{forceVector.y}) to {hit.pp.fixTrans.gameObject.name}");
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[Magnet] physicsCollider is null!");
                    }
                }
                else
                {
                    // Проверяем PlayerBody для игроков (строка 19484-19495)
                    var playerBody = hit.pp.fixTrans.GetComponent<PlayerBody>();
                    if (playerBody != null)
                    {
                        Fix playerPullStr = (Fix)50L;
                        Vec2 pullDirection = -staffDir;
                        playerBody.externalVelocity -= playerPullStr * pullDirection;
                        Plugin.Log.LogInfo($"[Magnet] Applied velocity to player");
                    }
                    else
                    {
                        Plugin.Log.LogWarning($"[Magnet] No BoplBody or PlayerBody found!");
                    }
                }
            }
        }

        private static void ApplyShrinkBeamEffect(Beam beam, Player player, PlayerBody body, Fix deltaTime)
        {
            // Проверяем что луч уже активен (не в стадии зарядки)
            var beamIndex = Traverse.Create(beam).Field("beamIndex").GetValue<int>();
            if (beamIndex < 0)
            {
                Plugin.Log.LogInfo($"[Shrink] Beam not active yet (beamIndex={beamIndex})");
                return;
            }

            var staffDir = Traverse.Create(beam).Field("staffDir").GetValue<Vec2>();
            Vec2 firePos = body.position + staffDir * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item", "wall");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, staffDir, maxDistance, collisionMask);

            Plugin.Log.LogInfo($"[Shrink] Raycast from ({firePos.x},{firePos.y}) dir ({staffDir.x},{staffDir.y}), hit={hit != null}");

            if (hit && hit.pp.fixTrans != null)
            {
                Plugin.Log.LogInfo($"[Shrink] Hit object: {hit.pp.fixTrans.gameObject.name}, layer={hit.pp.fixTrans.gameObject.layer}");

                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    Fix oldScale = targetBody.Scale;
                    // Уменьшаем напрямую как в ApplyGrowBeamEffect, но с отрицательным значением
                    Fix shrinkPerSecond = (Fix)0.2;
                    Fix shrinkThisFrame = shrinkPerSecond * deltaTime;
                    targetBody.Scale -= shrinkThisFrame;

                    // Ограничиваем минимальный размер
                    if (targetBody.Scale < (Fix)0.1)
                    {
                        targetBody.Scale = (Fix)0.1;
                    }

                    Plugin.Log.LogInfo($"[Shrink] Scale changed: {oldScale} -> {targetBody.Scale}");
                }
                else
                {
                    Plugin.Log.LogWarning($"[Shrink] No BoplBody found on {hit.pp.fixTrans.gameObject.name}");
                }
            }
        }
    }
}
