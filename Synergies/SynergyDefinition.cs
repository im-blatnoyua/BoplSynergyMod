using System.Collections.Generic;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Определяет синергию между двумя способностями
    /// </summary>
    public class SynergyDefinition
    {
        public string Ability1Name { get; set; } = "";
        public string Ability2Name { get; set; } = "";
        public string SynergyName { get; set; } = "";
        public SynergyType Type { get; set; }
    }

    public enum SynergyType
    {
        BeamDuplicate,      // Луч + размножение (3 луча)
        BeamGrow,           // Луч + увеличение (увеличивает объекты, уменьшает игрока)
        BeamTelekinesis     // Луч + перемещение (притягивание)
    }

    /// <summary>
    /// Трекер активных синергий для каждого игрока
    /// </summary>
    public static class SynergyTracker
    {
        // Хранит какие кнопки способностей нажаты для каждого игрока
        private static Dictionary<int, HashSet<int>> activeButtons = new Dictionary<int, HashSet<int>>();

        public static void SetButtonState(int playerId, int abilityIndex, bool isPressed)
        {
            if (!activeButtons.ContainsKey(playerId))
                activeButtons[playerId] = new HashSet<int>();

            if (isPressed)
                activeButtons[playerId].Add(abilityIndex);
            else
                activeButtons[playerId].Remove(abilityIndex);
        }

        public static bool AreBothPressed(int playerId, int ability1, int ability2)
        {
            if (!activeButtons.ContainsKey(playerId))
                return false;

            return activeButtons[playerId].Contains(ability1) &&
                   activeButtons[playerId].Contains(ability2);
        }

        public static void Clear(int playerId)
        {
            if (activeButtons.ContainsKey(playerId))
                activeButtons[playerId].Clear();
        }

        public static void ClearAll()
        {
            activeButtons.Clear();
        }
    }
}
