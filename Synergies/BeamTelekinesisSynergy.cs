using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace BoplSynergyMod.Synergies
{
    /// <summary>
    /// Синергия: Луч + Перемещение объектов
    /// Луч притягивает объекты к игроку
    /// </summary>
    public static class BeamTelekinesisSynergy
    {
        public static void Activate(SlimeController controller, Player player, int beamIndex, int magnetIndex)
        {
            Plugin.Log.LogInfo("[BeamTelekinesis] Activating synergy...");

            // Находим компоненты
            var beamAbility = FindAbilityByName(controller, beamIndex, magnetIndex, "beam");
            var magnetAbility = FindAbilityByName(controller, beamIndex, magnetIndex, "magnet");

            if (beamAbility == null || magnetAbility == null)
            {
                Plugin.Log.LogWarning("[BeamTelekinesis] Required abilities not found!");
                return;
            }

            // Получаем MagnetGun компонент
            var magnetGun = magnetAbility.GetComponent<MagnetGun>();
            if (magnetGun == null)
            {
                Plugin.Log.LogWarning("[BeamTelekinesis] MagnetGun not found!");
                return;
            }

            // Получаем направление и позицию
            Vec2 aimVector = player.AimVector();
            Vec2 firePos = controller.body.position + aimVector * (Fix)2.0;

            // Делаем raycast для поиска объектов
            Fix maxDistance = (Fix)100L;
            LayerMask collisionMask = LayerMask.GetMask("Default", "Player", "item");

            RaycastInformation hit = DetPhysics.Get().RaycastToClosest(firePos, aimVector, maxDistance, collisionMask);

            if (hit && hit.pp.fixTrans != null)
            {
                Plugin.Log.LogInfo($"[BeamTelekinesis] Hit object: {hit.pp.fixTrans.gameObject.name}");

                // Получаем BoplBody объекта
                var targetBody = hit.pp.fixTrans.GetComponent<BoplBody>();
                if (targetBody != null)
                {
                    // Вычисляем направление притягивания (от объекта к игроку)
                    Vec2 pullDirection = Vec2.NormalizedSafe(controller.body.position - targetBody.position);

                    // Применяем силу притягивания
                    Fix pullStrength = (Fix)5.0; // Сила притягивания
                    targetBody.velocity += pullDirection * pullStrength;

                    Plugin.Log.LogInfo($"[BeamTelekinesis] Pulling object with force {pullStrength}");

                    // Визуальный эффект (используем существующие частицы луча)
                    SpawnPullEffect(firePos, aimVector, hit.nearDist);
                }
                else
                {
                    Plugin.Log.LogInfo("[BeamTelekinesis] Target has no BoplBody");
                }
            }
            else
            {
                Plugin.Log.LogInfo("[BeamTelekinesis] No target hit");
            }

            // Устанавливаем кулдаун
            SetCooldown(controller, beamIndex, magnetIndex);
        }

        private static AbilityMonoBehaviour? FindAbilityByName(SlimeController controller, int index1, int index2, string nameContains)
        {
            var ability1 = controller.abilities[index1];
            var ability2 = controller.abilities[index2];

            if (ability1.gameObject.name.ToLower().Contains(nameContains))
                return ability1;
            if (ability2.gameObject.name.ToLower().Contains(nameContains))
                return ability2;

            return null;
        }

        private static void SpawnPullEffect(Vec2 firePos, Vec2 direction, Fix distance)
        {
            // Воспроизводим звук
            AudioManager.Get()?.Play("fireRaygun");

            // Можно добавить визуальные эффекты через существующие префабы
            // Но для простоты пока только звук
        }

        private static void SetCooldown(SlimeController controller, int index1, int index2)
        {
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
