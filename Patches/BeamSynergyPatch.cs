using BoplFixedMath;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BoplSynergyMod.Patches
{
    /// <summary>
    /// Патч на Beam.UpdateSim - проверяет синергии каждый кадр пока луч активен
    /// </summary>
    [HarmonyPatch(typeof(Beam), "UpdateSim")]
    public static class BeamSynergyPatch
    {
        private static Dictionary<int, bool> synergyActivated = new Dictionary<int, bool>();

        static void Postfix(Beam __instance, Fix SimDeltaTime)
        {
            try
            {
                // Получаем игрока
                var ability = Traverse.Create(__instance).Field("ability").GetValue<Ability>();
                if (ability == null)
                {
                    Plugin.Log.LogWarning("[BeamSynergy] ability is NULL");
                    return;
                }

                var playerInfo = ability.GetPlayerInfo();
                var player = PlayerHandler.Get().GetPlayer(playerInfo.playerId);
                if (player == null)
                {
                    Plugin.Log.LogWarning("[BeamSynergy] player is NULL");
                    return;
                }

                // Проверяем синергию только один раз за активацию луча
                if (synergyActivated.ContainsKey(player.Id) && synergyActivated[player.Id])
                    return;

                // Получаем body из Beam
                var body = Traverse.Create(__instance).Field("body").GetValue<PlayerBody>();
                if (body == null)
                {
                    Plugin.Log.LogWarning("[BeamSynergy] body is NULL");
                    return;
                }

                // Пробуем получить контроллер
                SlimeController controller = body.gameObject.GetComponent<SlimeController>();
                if (controller == null && body.transform.parent != null)
                    controller = body.transform.parent.GetComponent<SlimeController>();

                if (controller == null)
                {
                    Plugin.Log.LogWarning("[BeamSynergy] controller is NULL");
                    return;
                }

                Plugin.Log.LogInfo($"[BeamSynergy] Checking synergies for player {player.Id}, abilities count: {controller.abilities.Count}");

                // Проверяем какие ещё способности нажаты
                int pressedCount = 0;
                for (int i = 0; i < controller.abilities.Count; i++)
                {
                    if (player.AbilityButtonIsDown(i))
                    {
                        pressedCount++;
                        var otherAbility = controller.abilities[i];
                        string abilityName = otherAbility.gameObject.name.ToLower();

                        Plugin.Log.LogInfo($"[BeamSynergy] Button {i} pressed: {abilityName}");

                        // Проверяем синергии
                        if (abilityName.Contains("duplicat"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] DUPLICATE SYNERGY ACTIVATED!");
                            ApplyDuplicateSynergy(__instance, controller, player);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                        else if (abilityName.Contains("grow") || abilityName.Contains("scale"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] GROW SYNERGY ACTIVATED!");
                            ApplyGrowSynergy(__instance, controller, player, otherAbility);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                        else if (abilityName.Contains("magnet") || abilityName.Contains("telekin"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] MAGNET SYNERGY ACTIVATED!");
                            ApplyMagnetSynergy(__instance, controller, player);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                    }
                }

                if (pressedCount == 0)
                {
                    Plugin.Log.LogInfo("[BeamSynergy] No other buttons pressed");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[BeamSynergy] Error: {ex.Message}");
                Plugin.Log.LogError($"[BeamSynergy] Stack: {ex.StackTrace}");
            }
        }

        // Сбрасываем флаг когда луч заканчивается
        [HarmonyPatch(typeof(Beam), "ExitAbility", new System.Type[] { typeof(AbilityExitInfo) })]
        [HarmonyPostfix]
        static void OnBeamExit(Beam __instance)
        {
            try
            {
                var ability = Traverse.Create(__instance).Field("ability").GetValue<Ability>();
                if (ability != null)
                {
                    var playerInfo = ability.GetPlayerInfo();
                    synergyActivated[playerInfo.playerId] = false;
                }
            }
            catch { }
        }

        private static void ApplyDuplicateSynergy(Beam beam, SlimeController controller, Player player)
        {
            // Добавляем сильный recoil
            Vec2 aimVector = player.AimVector();
            Vec2 recoil = aimVector * (Fix)(-20.0);
            controller.body.selfImposedVelocity += recoil;

            // Получаем данные основного луча через Traverse
            var beamIndex = Traverse.Create(beam).Field("beamIndex").GetValue<int>();
            var timeSinceBeamStart = Traverse.Create(beam).Field("timeSinceBeamStart").GetValue<Fix>();
            var playerBeamColor = Traverse.Create(beam).Field("playerBeamColor").GetValue<DetPhysics.BeamColors>();
            var beamOffset = Traverse.Create(beam).Field("beamOffset").GetValue<Fix>();

            if (beamIndex < 0) return; // Луч ещё не активен

            // Создаём два дополнительных луча под углами ±45 градусов
            Vec2 position = controller.body.position + aimVector * beamOffset;
            Fix scale = player.Scale;

            // Угол 45 градусов = 0.785 радиан
            Fix angle45 = (Fix)0.785398;

            // Поворачиваем вектор на +45 градусов
            Vec2 direction1 = RotateVector(aimVector, angle45);
            // Поворачиваем вектор на -45 градусов
            Vec2 direction2 = RotateVector(aimVector, -angle45);

            // Добавляем два дополнительных луча в DetPhysics
            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction1,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam.HierarchyNumber + 1000, // Уникальный ID
                ownerId = player.Id
            });

            DetPhysics.Get().AddBeamBody(new DetPhysics.BeamBody
            {
                position = position,
                direction = direction2,
                scale = scale,
                colors = playerBeamColor,
                timePassed = timeSinceBeamStart,
                id = beam.HierarchyNumber + 2000, // Уникальный ID
                ownerId = player.Id
            });

            Plugin.Log.LogInfo("[BeamSynergy] Created 3 beams with recoil!");
        }

        // Поворот вектора на угол (в радианах)
        private static Vec2 RotateVector(Vec2 v, Fix angle)
        {
            Fix cos = Fix.Cos(angle);
            Fix sin = Fix.Sin(angle);
            return new Vec2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        private static void ApplyGrowSynergy(Beam beam, SlimeController controller, Player player, AbilityMonoBehaviour growAbility)
        {
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "item");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                // Увеличиваем объект постепенно
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Рост: +0.2 за секунду, за 5 секунд = x2
                    Fix growthPerSecond = (Fix)0.2;
                    Fix growthThisFrame = growthPerSecond * (Fix)0.016; // ~60 FPS
                    targetBody.Scale += growthThisFrame;
                }
            }

            // Уменьшаем игрока постепенно
            if (player.Scale > (Fix)0.3)
            {
                // Уменьшение: -0.1 за секунду, за 5 секунд = /2
                Fix shrinkPerSecond = (Fix)0.1;
                Fix shrinkThisFrame = shrinkPerSecond * (Fix)0.016; // ~60 FPS
                player.Scale = Fix.Max(player.Scale - shrinkThisFrame, (Fix)0.3);
            }
        }

        private static void ApplyMagnetSynergy(Beam beam, SlimeController controller, Player player)
        {
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Raycast
            Fix maxDistance = (Fix)100L;
            LayerMask mask = LayerMask.GetMask("Default", "item", "wall");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, mask);

            if (hit && hit.pp.fixTrans != null)
            {
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Используем ту же логику что и луч с черной дырой
                    // Сила притяжения зависит от Scale объекта
                    Fix beamPushForce = (Fix)50L; // Базовая сила
                    Fix scaleMultiplier = Fix.Min(player.Scale, (Fix)40L);

                    // Направление от луча к объекту (отталкивание)
                    Vec2 pushDirection = aimVector;

                    // Применяем силу с учетом массы (как AddForceLessMassInfluence)
                    // Для островов с отрицательным Scale это будет притяжение!
                    Fix massSign = (Fix)Fix.Sign2(targetBody.Scale);
                    Vec2 force = massSign * pushDirection * beamPushForce * scaleMultiplier;

                    // Делим на sqrt(abs(scale)) для уменьшения влияния массы
                    Fix scaleFactor = Fix.Sqrt(Fix.Abs(targetBody.Scale));
                    if (scaleFactor > Fix.Zero)
                    {
                        targetBody.velocity += force / scaleFactor * (Fix)0.016;
                        Plugin.Log.LogInfo($"[BeamSynergy] Applying force to object! Scale: {targetBody.Scale}, Force: {force}");
                    }
                }
            }
        }
    }
}
