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

            // Используем БОЛЬШИЕ отрицательные ID чтобы они точно не совпали с ID игроков
            // ID игроков обычно 0-3, используем -10000 и меньше
            int beam1Id = -10000 - player.Id * 100 - 1;
            int beam2Id = -10000 - player.Id * 100 - 2;

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction1,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam1Id,
                ownerId = player.Id,
                ground = currentGround
            });

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction2,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam2Id,
                ownerId = player.Id,
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
            if (beamIndex < 0) return; // Луч ещё заряжается

            var staffDir = Traverse.Create(beam).Field("staffDir").GetValue<Vec2>();
            Vec2 firePos = body.position + staffDir * (Fix)2.0;

            Fix maxDistance = (Fix)100L;
            LayerMask mask = LayerMask.GetMask("Default", "item", "wall", "Projectile");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, staffDir, maxDistance, mask);

            if (hit && hit.pp.fixTrans != null)
            {
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

                    // Применяем силу напрямую через velocity (как в MagnetGun строка 19449)
                    targetBody.velocity += force * pullDirection * deltaTime;
                }
                else
                {
                    // Проверяем PlayerBody для игроков
                    var playerBody = hit.pp.fixTrans.GetComponent<PlayerBody>();
                    if (playerBody != null)
                    {
                        Fix playerPullStr = (Fix)50L;
                        Vec2 pullDirection = -staffDir;
                        playerBody.externalVelocity += playerPullStr * pullDirection * deltaTime;
                    }
                }
            }
        }

        private static void ApplyShrinkBeamEffect(Beam beam, Player player, PlayerBody body, Fix deltaTime)
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
                    // Реверс увеличения: уменьшаем с той же скоростью но в обратную сторону
                    Fix shrinkPerSecond = (Fix)(-0.2); // Отрицательное значение для уменьшения
                    Fix shrinkThisFrame = shrinkPerSecond * deltaTime;

                    // Применяем изменение с минимальным размером 0.1
                    Fix newScale = targetBody.Scale + shrinkThisFrame;
                    if (newScale > (Fix)0.1)
                    {
                        targetBody.Scale = newScale;
                    }
                    else
                    {
                        targetBody.Scale = (Fix)0.1; // Не даём стать меньше минимума
                    }
                }
            }
        }
    }
}
