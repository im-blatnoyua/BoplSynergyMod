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

            // Проверяем синергии
            if (TryActivateSynergy(__instance, player))
            {
                // Синергия активирована, блокируем стандартное поведение
                return false;
            }

            // Продолжаем стандартное поведение
            return true;
        }

        private static bool TryActivateSynergy(SlimeController controller, Player player)
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
                        // Определяем тип синергии
                        var synergyType = GetSynergyType(abilities[i], abilities[j]);

                        if (synergyType != null)
                        {
                            Plugin.Log.LogInfo($"[Synergy] Player {playerId} activated synergy: {synergyType}");
                            ActivateSynergy(controller, player, i, j, synergyType.Value);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static Synergies.SynergyType? GetSynergyType(AbilityMonoBehaviour ability1, AbilityMonoBehaviour ability2)
        {
            string name1 = ability1.gameObject.name.ToLower();
            string name2 = ability2.gameObject.name.ToLower();

            // Beam + Duplicate
            if ((name1.Contains("beam") && name2.Contains("duplicat")) ||
                (name2.Contains("beam") && name1.Contains("duplicat")))
            {
                return Synergies.SynergyType.BeamDuplicate;
            }

            // Beam + Grow/Scale
            if ((name1.Contains("beam") && (name2.Contains("grow") || name2.Contains("scale"))) ||
                (name2.Contains("beam") && (name1.Contains("grow") || name1.Contains("scale"))))
            {
                return Synergies.SynergyType.BeamGrow;
            }

            // Beam + Telekinesis/Magnet
            if ((name1.Contains("beam") && (name2.Contains("magnet") || name2.Contains("telekin"))) ||
                (name2.Contains("beam") && (name1.Contains("magnet") || name1.Contains("telekin"))))
            {
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
