using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Размножение
    /// Создает 3 луча под разными углами (центр, +45°, -45°)
    /// </summary>
    public static class BeamDuplicateSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int duplicateIndex)
        {
            Plugin.Log.LogInfo("[BeamDuplicate] Activating synergy...");

            // Находим компонент Beam
            var beamAbility = FindBeamAbility(controller, beamIndex, duplicateIndex);
            if (beamAbility == null)
            {
                Plugin.Log.LogWarning("[BeamDuplicate] Beam ability not found!");
                return;
            }

            // Получаем GunTransform который стреляет лучом
            var gunTransform = beamAbility.GetComponent<GunTransform>();
            if (gunTransform == null)
            {
                Plugin.Log.LogWarning("[BeamDuplicate] GunTransform not found!");
                return;
            }

            // Получаем IGun компонент для стрельбы
            var gun = Traverse.Create(gunTransform).Field("gun").GetValue<IGun>();
            if (gun == null)
            {
                Plugin.Log.LogWarning("[BeamDuplicate] IGun not found!");
                return;
            }

            // Получаем направление стрельбы
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Стреляем центральным лучом
            bool hasFired = false;
            gun.Shoot(firePos, aimVector, ref hasFired, player.Id, false);

            // Стреляем лучом под углом +45 градусов
            Vec2 rotatedRight = RotateVector(aimVector, (Fix)0.785); // 45° в радианах
            gun.Shoot(firePos, rotatedRight, ref hasFired, player.Id, false);

            // Стреляем лучом под углом -45 градусов
            Vec2 rotatedLeft = RotateVector(aimVector, (Fix)(-0.785));
            gun.Shoot(firePos, rotatedLeft, ref hasFired, player.Id, false);

            // Применяем recoil (отталкивание)
            Vec2 recoil = aimVector * (Fix)(-3.0); // Отталкивание назад
            controller.body.selfImposedVelocity += recoil;

            Plugin.Log.LogInfo("[BeamDuplicate] Synergy activated successfully!");

            // Устанавливаем кулдаун
            SetCooldown(controller, beamIndex, duplicateIndex);
        }

        private static AbilityMonoBehaviour? FindBeamAbility(SlimeController controller, int index1, int index2)
        {
            var ability1 = controller.abilities[index1];
            var ability2 = controller.abilities[index2];

            if (ability1.gameObject.name.ToLower().Contains("beam"))
                return ability1;
            if (ability2.gameObject.name.ToLower().Contains("beam"))
                return ability2;

            return null;
        }

        private static Vec2 RotateVector(Vec2 v, Fix angleRadians)
        {
            Fix cos = Fix.Cos(angleRadians);
            Fix sin = Fix.Sin(angleRadians);
            return new Vec2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos
            );
        }

        private static void SetCooldown(SlimeController controller, int index1, int index2)
        {
            // Используем рефлексию для доступа к приватному полю
            var cooldownField = Traverse.Create(controller).Field("abilityCooldownTimers");
            var cooldowns = cooldownField.GetValue<Fix[]>();

            if (cooldowns != null)
            {
                cooldowns[index1] = Fix.Zero;
                cooldowns[index2] = Fix.Zero;
            }
        }
    }
}
