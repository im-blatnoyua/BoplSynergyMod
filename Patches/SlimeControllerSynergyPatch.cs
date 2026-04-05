using BoplFixedMath;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace BoplSynergyMod.Patches
{
    [HarmonyPatch(typeof(SlimeController), "OldUpdate")]
    public static class SlimeControllerSynergyPatch
    {
        private static Dictionary<int, bool> synergyActiveThisFrame = new Dictionary<int, bool>();
        private static Dictionary<int, int> activeBeamSynergy = new Dictionary<int, int>(); // playerId -> synergy type

        static bool Prefix(SlimeController __instance, Fix simDeltaTime)
        {
            try
            {
                bool isInAbility = Traverse.Create(__instance).Field("isInAbility").GetValue<bool>();
                if (isInAbility) return true;

                Player player = PlayerHandler.Get().GetPlayer(__instance.playerNumber);
                if (player == null) return true;

                int playerId = player.Id;

                if (!synergyActiveThisFrame.ContainsKey(playerId))
                    synergyActiveThisFrame[playerId] = false;

                List<int> pressedButtons = new List<int>();
                for (int i = 0; i < __instance.abilities.Count; i++)
                {
                    if (player.AbilityButtonIsDown(i) &&
                        isAbilityReady(__instance, i) &&
                        isAbilityCastable(__instance, player, i))
                    {
                        pressedButtons.Add(i);
                    }
                }

                if (pressedButtons.Count == 2 && !synergyActiveThisFrame[playerId])
                {
                    int button1 = pressedButtons[0];
                    int button2 = pressedButtons[1];

                    string ability1 = __instance.abilities[button1].gameObject.name.ToLower();
                    string ability2 = __instance.abilities[button2].gameObject.name.ToLower();

                    Plugin.Log.LogInfo($"[Synergy] Player {playerId} pressed {ability1} + {ability2}");

                    if (CheckSynergy(ability1, ability2, "beam", "duplicat"))
                    {
                        Plugin.Log.LogInfo("[Synergy] BEAM + DUPLICATE activated!");
                        ApplyBeamDuplicateSynergy(__instance, player, button1, button2);
                        synergyActiveThisFrame[playerId] = true;
                        activeBeamSynergy[playerId] = 1;
                        return false;
                    }
                    else if (CheckSynergy(ability1, ability2, "beam", "grow") ||
                             CheckSynergy(ability1, ability2, "beam", "shootscale"))
                    {
                        Plugin.Log.LogInfo("[Synergy] BEAM + GROW activated!");
                        ApplyBeamGrowSynergy(__instance, player, button1, button2);
                        synergyActiveThisFrame[playerId] = true;
                        activeBeamSynergy[playerId] = 2;
                        return false;
                    }
                    else if (CheckSynergy(ability1, ability2, "beam", "magnet") ||
                             CheckSynergy(ability1, ability2, "beam", "telekin"))
                    {
                        Plugin.Log.LogInfo("[Synergy] BEAM + MAGNET activated!");
                        ApplyBeamMagnetSynergy(__instance, player, button1, button2);
                        synergyActiveThisFrame[playerId] = true;
                        activeBeamSynergy[playerId] = 3;
                        return false;
                    }
                    else if (CheckSynergy(ability1, ability2, "beam", "scalechanger"))
                    {
                        Plugin.Log.LogInfo("[Synergy] BEAM + SHRINK activated!");
                        ApplyBeamShrinkSynergy(__instance, player, button1, button2);
                        synergyActiveThisFrame[playerId] = true;
                        activeBeamSynergy[playerId] = 4;
                        return false;
                    }
                }

                if (pressedButtons.Count < 2)
                {
                    synergyActiveThisFrame[playerId] = false;
                    activeBeamSynergy.Remove(playerId);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[Synergy] Error: {ex.Message}\n{ex.StackTrace}");
                return true;
            }
        }

        private static bool isAbilityReady(SlimeController controller, int abilityIndex)
        {
            var timers = Traverse.Create(controller).Field("abilityCooldownTimers").GetValue<Fix[]>();
            return timers[abilityIndex] > controller.abilities[abilityIndex].GetCooldown();
        }

        private static bool isAbilityCastable(SlimeController controller, Player player, int abilityIndex)
        {
            var ability = controller.abilities[abilityIndex];
            if (ability.OnlyCastableOnGround && !controller.playerPhysics.IsGrounded())
                return false;
            if (!player.CanUseAbilities)
                return false;
            return ability.IsCastable(player);
        }

        private static bool CheckSynergy(string ability1, string ability2, string need1, string need2)
        {
            return (ability1.Contains(need1) && ability2.Contains(need2)) ||
                   (ability1.Contains(need2) && ability2.Contains(need1));
        }

        private static void ApplyBeamDuplicateSynergy(SlimeController controller, Player player, int button1, int button2)
        {
            int beamIndex = FindAbilityIndex(controller, "beam");
            if (beamIndex == -1) return;

            Traverse.Create(controller).Method("EnterAbility", beamIndex, false).GetValue();

            Vec2 aimVector = player.AimVector();
            Vec2 recoil = aimVector * (Fix)(-30.0);
            controller.body.selfImposedVelocity += recoil;

            SetCooldown(controller, button1);
            SetCooldown(controller, button2);

            Plugin.Log.LogInfo("[Synergy] Applied Beam+Duplicate: triple beam with recoil");
        }

        private static void ApplyBeamGrowSynergy(SlimeController controller, Player player, int button1, int button2)
        {
            int beamIndex = FindAbilityIndex(controller, "beam");
            if (beamIndex == -1) return;

            Traverse.Create(controller).Method("EnterAbility", beamIndex, false).GetValue();

            SetCooldown(controller, button1);
            SetCooldown(controller, button2);

            Plugin.Log.LogInfo("[Synergy] Applied Beam+Grow: will grow/shrink objects");
        }

        private static void ApplyBeamMagnetSynergy(SlimeController controller, Player player, int button1, int button2)
        {
            int beamIndex = FindAbilityIndex(controller, "beam");
            if (beamIndex == -1) return;

            Traverse.Create(controller).Method("EnterAbility", beamIndex, false).GetValue();

            SetCooldown(controller, button1);
            SetCooldown(controller, button2);

            Plugin.Log.LogInfo("[Synergy] Applied Beam+Magnet: will push/pull objects");
        }

        private static void ApplyBeamShrinkSynergy(SlimeController controller, Player player, int button1, int button2)
        {
            int beamIndex = FindAbilityIndex(controller, "beam");
            if (beamIndex == -1) return;

            Traverse.Create(controller).Method("EnterAbility", beamIndex, false).GetValue();

            SetCooldown(controller, button1);
            SetCooldown(controller, button2);

            Plugin.Log.LogInfo("[Synergy] Applied Beam+Shrink: will shrink objects");
        }

        private static void SetCooldown(SlimeController controller, int abilityIndex)
        {
            var timers = Traverse.Create(controller).Field("abilityCooldownTimers").GetValue<Fix[]>();
            timers[abilityIndex] = Fix.Zero;
        }

        private static int FindAbilityIndex(SlimeController controller, string abilityName)
        {
            for (int i = 0; i < controller.abilities.Count; i++)
            {
                if (controller.abilities[i].gameObject.name.ToLower().Contains(abilityName))
                    return i;
            }
            return -1;
        }

        public static int GetActiveSynergy(int playerId)
        {
            return activeBeamSynergy.ContainsKey(playerId) ? activeBeamSynergy[playerId] : 0;
        }
    }
}
