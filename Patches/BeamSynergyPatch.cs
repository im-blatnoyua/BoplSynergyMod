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
                // Логируем время луча (только один раз)
                var timeSinceBeamStart = Traverse.Create(__instance).Field("timeSinceBeamStart").GetValue<Fix>();
                if (timeSinceBeamStart == Fix.Zero)
                {
                    var maxTime = Traverse.Create(__instance).Field("maxTime").GetValue<Fix>();
                    var maxTimeAir = Traverse.Create(__instance).Field("maxTimeAir").GetValue<Fix>();
                    Plugin.Log.LogInfo($"[BeamSynergy] Beam maxTime: {maxTime} ({(float)maxTime}s), maxTimeAir: {maxTimeAir} ({(float)maxTimeAir}s)");
                }

                // Получаем игрока
                var ability = Traverse.Create(__instance).Field("ability").GetValue<Ability>();
                if (ability == null) return;

                var playerInfo = ability.GetPlayerInfo();
                var player = PlayerHandler.Get().GetPlayer(playerInfo.playerId);
                if (player == null) return;

                // Проверяем синергию только один раз за активацию луча
                if (synergyActivated.ContainsKey(player.Id) && synergyActivated[player.Id])
                    return;

                // Получаем body из Beam
                var body = Traverse.Create(__instance).Field("body").GetValue<PlayerBody>();
                if (body == null) return;

                // Пробуем получить контроллер
                SlimeController controller = body.gameObject.GetComponent<SlimeController>();
                if (controller == null && body.transform.parent != null)
                    controller = body.transform.parent.GetComponent<SlimeController>();

                if (controller == null) return;

                Plugin.Log.LogInfo($"[BeamSynergy] Checking synergies for player {player.Id}");

                // Проверяем какие ещё способности нажаты
                for (int i = 0; i < controller.abilities.Count; i++)
                {
                    if (player.AbilityButtonIsDown(i))
                    {
                        var otherAbility = controller.abilities[i];
                        string abilityName = otherAbility.gameObject.name.ToLower();

                        Plugin.Log.LogInfo($"[BeamSynergy] Button {i} pressed: {abilityName}");

                        // Проверяем синергии
                        if (abilityName.Contains("duplicat"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] DUPLICATE SYNERGY!");
                            ApplyDuplicateSynergy(__instance, controller, player);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                        else if (abilityName.Contains("grow") || abilityName.Contains("scale"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] GROW SYNERGY!");
                            ApplyGrowSynergy(__instance, controller, player, otherAbility);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                        else if (abilityName.Contains("magnet"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] MAGNET SYNERGY!");
                            ApplyMagnetSynergy(__instance, controller, player);
                            synergyActivated[player.Id] = true;
                            return;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[BeamSynergy] Error: {ex.Message}");
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

            Plugin.Log.LogInfo("[BeamSynergy] Applied recoil!");
        }

        private static void ApplyGrowSynergy(Beam beam, SlimeController controller, Player player, AbilityMonoBehaviour growAbility)
        {
            // Находим ShootScaleChange
            var scaleGun = growAbility.GetComponent<ShootScaleChange>();
            if (scaleGun == null)
            {
                Plugin.Log.LogWarning("[BeamSynergy] No ShootScaleChange found!");
                return;
            }

            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Стреляем лучом увеличения
            bool hasFired = false;
            scaleGun.Shoot(firePos, aimVector, ref hasFired, player.Id);

            // Уменьшаем игрока
            if (player.Scale > (Fix)0.3)
            {
                player.Scale = Fix.Max(player.Scale * (Fix)0.9, (Fix)0.3);
                Plugin.Log.LogInfo($"[BeamSynergy] Player scale: {player.Scale}");
            }
        }

        private static void ApplyMagnetSynergy(Beam beam, SlimeController controller, Player player)
        {
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Raycast
            Fix maxDistance = (Fix)100L;
            LayerMask mask = LayerMask.GetMask("Default", "item");
            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, mask);

            if (hit && hit.pp.fixTrans != null)
            {
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Притягиваем
                    Vec2 direction = controller.body.position - targetBody.position;
                    Fix distance = Vec2.Magnitude(direction);

                    if (distance > (Fix)0.3)
                    {
                        Vec2 normalized = direction / distance;
                        Fix force = (Fix)1000L / (distance * distance);
                        targetBody.velocity += normalized * force * (Fix)0.016;

                        Plugin.Log.LogInfo("[BeamSynergy] Pulling object!");
                    }
                }
            }
        }
    }
}
