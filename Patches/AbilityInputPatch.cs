using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Patches
{
    /// <summary>
    /// Патч для перехвата нажатий кнопок способностей
    /// </summary>
    [HarmonyPatch(typeof(SlimeController), "OldUpdate")]
    public static class AbilityInputPatch
    {
        static bool Prefix(SlimeController __instance, Fix simDeltaTime)
        {
            // Получаем игрока
            int playerId = __instance.GetPlayerId();
            Player player = PlayerHandler.Get().GetPlayer(playerId);

            if (player == null || __instance.suppressInput)
                return true;

            // Проверяем нажатия кнопок способностей
            for (int i = 0; i < __instance.abilities.Count; i++)
            {
                bool isPressed = player.AbilityButtonIsDown(i);
                Synergies.SynergyTracker.SetButtonState(playerId, i, isPressed);
            }

            // Проверяем синергии (но НЕ блокируем стандартное поведение)
            TryActivateSynergy(__instance, player);

            // Всегда продолжаем стандартное поведение
            return true;
        }

        private static void TryActivateSynergy(SlimeController controller, Player player)
        {
            int playerId = player.Id;
            var abilities = controller.abilities;

            // Проверяем все пары способностей
            for (int i = 0; i < abilities.Count; i++)
            {
                for (int j = i + 1; j < abilities.Count; j++)
                {
                    if (Synergies.SynergyTracker.AreBothPressed(playerId, i, j))
                    {
                        Plugin.Log.LogInfo($"[Synergy] Both buttons pressed: {i} and {j}");

                        // Определяем тип синергии
                        var synergyType = GetSynergyType(abilities[i], abilities[j]);

                        if (synergyType != null)
                        {
                            Plugin.Log.LogInfo($"[Synergy] Player {playerId} activated synergy: {synergyType}");
                            ActivateSynergy(controller, player, i, j, synergyType.Value);

                            // Устанавливаем кулдаун на обе способности
                            SetCooldown(controller, i, j);
                            return;
                        }
                        else
                        {
                            Plugin.Log.LogInfo($"[Synergy] No synergy found for: {abilities[i].gameObject.name} + {abilities[j].gameObject.name}");
                        }
                    }
                }
            }
        }

        private static void SetCooldown(SlimeController controller, int index1, int index2)
        {
            try
            {
                var cooldownField = Traverse.Create(controller).Field("abilityCooldownTimers");
                var cooldowns = cooldownField.GetValue<Fix[]>();

                if (cooldowns != null && cooldowns.Length > index1 && cooldowns.Length > index2)
                {
                    // Получаем максимальный кулдаун из обеих способностей
                    var ability1 = controller.abilities[index1];
                    var ability2 = controller.abilities[index2];

                    Fix cooldown1 = Traverse.Create(ability1).Field("Cooldown").GetValue<Fix>();
                    Fix cooldown2 = Traverse.Create(ability2).Field("Cooldown").GetValue<Fix>();

                    Fix maxCooldown = Fix.Max(cooldown1, cooldown2);

                    cooldowns[index1] = maxCooldown;
                    cooldowns[index2] = maxCooldown;

                    Plugin.Log.LogInfo($"[Synergy] Set cooldown {maxCooldown} on abilities {index1} and {index2}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[Synergy] Failed to set cooldown: {ex.Message}");
            }
        }

        private static Synergies.SynergyType? GetSynergyType(AbilityMonoBehaviour ability1, AbilityMonoBehaviour ability2)
        {
            string name1 = ability1.gameObject.name.ToLower();
            string name2 = ability2.gameObject.name.ToLower();

            Plugin.Log.LogInfo($"[Synergy] Checking: '{name1}' + '{name2}'");

            // Beam + Duplicate
            if ((name1.Contains("beam") && name2.Contains("duplicat")) ||
                (name2.Contains("beam") && name1.Contains("duplicat")))
            {
                Plugin.Log.LogInfo("[Synergy] Found BeamDuplicate synergy!");
                return Synergies.SynergyType.BeamDuplicate;
            }

            // Beam + Grow/Scale
            if ((name1.Contains("beam") && (name2.Contains("grow") || name2.Contains("scale"))) ||
                (name2.Contains("beam") && (name1.Contains("grow") || name1.Contains("scale"))))
            {
                Plugin.Log.LogInfo("[Synergy] Found BeamGrow synergy!");
                return Synergies.SynergyType.BeamGrow;
            }

            // Beam + Telekinesis/Magnet
            if ((name1.Contains("beam") && (name2.Contains("magnet") || name2.Contains("telekin"))) ||
                (name2.Contains("beam") && (name1.Contains("magnet") || name1.Contains("telekin"))))
            {
                Plugin.Log.LogInfo("[Synergy] Found BeamTelekinesis synergy!");
                return Synergies.SynergyType.BeamTelekinesis;
            }

            return null;
        }

        private static void ActivateSynergy(SlimeController controller, Player player, int ability1Index, int ability2Index, Synergies.SynergyType synergyType)
        {
            switch (synergyType)
            {
                case Synergies.SynergyType.BeamDuplicate:
                    Synergies.BeamDuplicateSynergy.Activate(controller, player, ability1Index, ability2Index);
                    break;
                case Synergies.SynergyType.BeamGrow:
                    Synergies.BeamGrowSynergy.Activate(controller, player, ability1Index, ability2Index);
                    break;
                case Synergies.SynergyType.BeamTelekinesis:
                    Synergies.BeamTelekinesisSynergy.Activate(controller, player, ability1Index, ability2Index);
                    break;
            }
        }
    }
}
