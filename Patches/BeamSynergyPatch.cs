using BoplFixedMath;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BoplSynergyMod.Patches
{
    /// <summary>
    /// Патч на Beam.OnEnterAbility - добавляет синергии к лучу
    /// </summary>
    [HarmonyPatch(typeof(Beam), "OnEnterAbility")]
    public static class BeamSynergyPatch
    {
        static void Postfix(Beam __instance)
        {
            try
            {
                Plugin.Log.LogInfo("[BeamSynergy] Beam activated!");

                // Получаем игрока
                var ability = Traverse.Create(__instance).Field("ability").GetValue<Ability>();
                Plugin.Log.LogInfo($"[BeamSynergy] Ability: {(ability != null ? "OK" : "NULL")}");
                if (ability == null) return;

                var playerInfo = ability.GetPlayerInfo();
                Plugin.Log.LogInfo($"[BeamSynergy] PlayerInfo playerId: {playerInfo.playerId}");

                var player = PlayerHandler.Get().GetPlayer(playerInfo.playerId);
                Plugin.Log.LogInfo($"[BeamSynergy] Player: {(player != null ? player.Id.ToString() : "NULL")}");
                if (player == null) return;

                // Получаем body из Beam
                var body = Traverse.Create(__instance).Field("body").GetValue<PlayerBody>();
                Plugin.Log.LogInfo($"[BeamSynergy] Body: {(body != null ? "OK" : "NULL")}");
                if (body == null) return;

                // Пробуем получить контроллер разными способами
                SlimeController controller = null;

                // Способ 1: из body.gameObject
                controller = body.gameObject.GetComponent<SlimeController>();
                Plugin.Log.LogInfo($"[BeamSynergy] Controller from body.gameObject: {(controller != null ? "OK" : "NULL")}");

                // Способ 2: из body.transform.parent
                if (controller == null && body.transform.parent != null)
                {
                    controller = body.transform.parent.GetComponent<SlimeController>();
                    Plugin.Log.LogInfo($"[BeamSynergy] Controller from parent: {(controller != null ? "OK" : "NULL")}");
                }

                // Способ 3: из playerInfo
                if (controller == null)
                {
                    var slimeController = Traverse.Create(playerInfo).Field("slimeController").GetValue<SlimeController>();
                    controller = slimeController;
                    Plugin.Log.LogInfo($"[BeamSynergy] Controller from playerInfo: {(controller != null ? "OK" : "NULL")}");
                }

                if (controller == null)
                {
                    Plugin.Log.LogWarning("[BeamSynergy] Could not find controller!");
                    return;
                }

                Plugin.Log.LogInfo($"[BeamSynergy] Player {player.Id} abilities count: {controller.abilities.Count}");

                // Проверяем какие ещё способности нажаты
                for (int i = 0; i < controller.abilities.Count; i++)
                {
                    bool isPressed = player.AbilityButtonIsDown(i);
                    Plugin.Log.LogInfo($"[BeamSynergy] Ability {i} pressed: {isPressed}");

                    if (isPressed)
                    {
                        var otherAbility = controller.abilities[i];
                        string abilityName = otherAbility.gameObject.name.ToLower();

                        Plugin.Log.LogInfo($"[BeamSynergy] Button {i} pressed: {abilityName}");

                        // Проверяем синергии
                        if (abilityName.Contains("duplicat"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] DUPLICATE SYNERGY!");
                            ApplyDuplicateSynergy(__instance, controller, player);
                        }
                        else if (abilityName.Contains("grow") || abilityName.Contains("scale"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] GROW SYNERGY!");
                            ApplyGrowSynergy(__instance, controller, player, otherAbility);
                        }
                        else if (abilityName.Contains("magnet"))
                        {
                            Plugin.Log.LogInfo("[BeamSynergy] MAGNET SYNERGY!");
                            ApplyMagnetSynergy(__instance, controller, player);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[BeamSynergy] Error: {ex.Message}");
                Plugin.Log.LogError($"[BeamSynergy] Stack: {ex.StackTrace}");
            }
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
