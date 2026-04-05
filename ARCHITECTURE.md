# Bopl Battle Synergy Mod - Архитектура

## Обзор

Мод добавляет систему синергий между способностями в Bopl Battle. При одновременном нажатии двух кнопок способностей активируется специальный комбинированный эффект.

## Реализованные синергии

### 1. Луч + Размножение (Beam + Duplicate)
- **Эффект**: Стреляет 3 лучами одновременно (центр, +45°, -45°)
- **Дополнительно**: Применяет recoil (отталкивание игрока назад)
- **Реализация**: Использует существующий `IGun.Shoot()` метод 3 раза с разными углами

### 2. Луч + Увеличение (Beam + Grow)
- **Эффект**: Луч увеличивает объекты, одновременно уменьшая игрока
- **Ограничение**: Игрок не может стать меньше минимального размера (0.3)
- **Реализация**: Использует `ShootScaleChange.Shoot()` + модификация `Player.Scale`

### 3. Луч + Перемещение (Beam + Telekinesis)
- **Эффект**: Луч притягивает объекты к игроку
- **Механика**: Raycast + применение силы к `BoplBody.velocity`
- **Реализация**: Использует `DetPhysics.RaycastToClosest()` + модификация velocity

## Архитектура

### Структура файлов

```
BoplSynergyMod/
├── Plugin.cs                           # Точка входа BepInEx
├── Patches/
│   └── AbilityInputPatch.cs           # Перехват нажатий кнопок
├── Synergies/
│   ├── SynergyDefinition.cs           # Определения и трекер синергий
│   ├── BeamDuplicateSynergy.cs        # Реализация синергии 1
│   ├── BeamGrowSynergy.cs             # Реализация синергии 2
│   └── BeamTelekinesisSynergy.cs      # Реализация синергии 3
└── Decompiled/                         # Декомпилированный код игры (для справки)
```

### Ключевые компоненты

#### 1. SynergyTracker
Отслеживает состояние нажатых кнопок для каждого игрока:
```csharp
Dictionary<int, HashSet<int>> activeButtons
```

#### 2. AbilityInputPatch
Harmony Prefix патч на `SlimeController.OldUpdate()`:
- Перехватывает input перед стандартной обработкой
- Проверяет комбинации нажатых кнопок
- Активирует синергию если найдена
- Возвращает `false` чтобы заблокировать стандартное поведение

#### 3. Synergy Implementations
Каждая синергия - статический класс с методом `Activate()`:
- Получает ссылки на нужные компоненты способностей
- Вызывает существующие методы игры (НЕ переписывает логику)
- Устанавливает кулдауны через Harmony Traverse

## Используемые классы игры

### Input система
- `Player.AbilityButtonIsDown(int index)` - проверка нажатия кнопки
- `Player.InputProfile.firstAbilityButton_IS_DOWN` - состояние кнопки 1
- `Player.InputProfile.secondAbilityButton_IS_DOWN` - состояние кнопки 2
- `Player.InputProfile.thirdAbilityButton_IS_DOWN` - состояние кнопки 3

### Способности
- `SlimeController.abilities` - список способностей игрока
- `SlimeController.EnterAbility(int index, bool useRope)` - активация способности
- `AbilityMonoBehaviour` - базовый класс всех способностей

### Стрельба
- `IGun.Shoot(Vec2 firePos, Vec2 direction, ref bool hasFired, int playerId)` - универсальный метод стрельбы
- `ShootScaleChange` - луч увеличения/уменьшения
- `ShootDuplicator` - луч размножения
- `GunTransform` - обертка для оружия

### Физика
- `DetPhysics.RaycastToClosest()` - raycast для поиска объектов
- `BoplBody.velocity` - скорость физического объекта
- `PlayerBody.selfImposedVelocity` - скорость игрока
- `Vec2` - фиксированная математика для векторов

### Утилиты
- `Fix` - fixed-point математика (детерминистичная)
- `PlayerHandler.Get().GetPlayer(int id)` - получение игрока по ID
- `AudioManager.Get().Play(string sound)` - воспроизведение звуков

## Как работает перехват input

1. **InputUpdater.OnAbility0/1/2()** - Unity Input System вызывает эти методы
2. Устанавливается `inputProfile.firstAbilityButton_IS_DOWN = true`
3. **SlimeController.OldUpdate()** - основной игровой цикл
4. **AbilityInputPatch.Prefix** перехватывает вызов
5. Проверяет `Player.AbilityButtonIsDown(i)` для всех способностей
6. Обновляет `SynergyTracker.SetButtonState()`
7. Проверяет `SynergyTracker.AreBothPressed()` для всех пар
8. Если синергия найдена - активирует и возвращает `false` (блокирует оригинал)
9. Если нет - возвращает `true` (продолжает стандартное поведение)

## Доступ к приватным полям

Используется Harmony Traverse для доступа к приватным полям:

```csharp
var cooldownField = Traverse.Create(controller).Field("abilityCooldownTimers");
var cooldowns = cooldownField.GetValue<Fix[]>();
cooldowns[index] = Fix.Zero;
```

## Кулдауны

После активации синергии устанавливаем кулдауны обеих способностей в 0:
```csharp
abilityCooldownTimers[ability1Index] = Fix.Zero;
abilityCooldownTimers[ability2Index] = Fix.Zero;
```

Это заставляет игрока ждать полного кулдауна перед следующим использованием.

## Определение типа способности

Проверяем имя GameObject способности:
```csharp
string name = ability.gameObject.name.ToLower();
if (name.Contains("beam")) { ... }
if (name.Contains("grow")) { ... }
if (name.Contains("duplicat")) { ... }
if (name.Contains("magnet")) { ... }
```

## Математика углов

Для поворота вектора используется матрица поворота:
```csharp
Vec2 RotateVector(Vec2 v, Fix angleRadians)
{
    Fix cos = Fix.Cos(angleRadians);
    Fix sin = Fix.Sin(angleRadians);
    return new Vec2(
        v.x * cos - v.y * sin,
        v.x * sin + v.y * cos
    );
}
```

45° = 0.785 радиан

## Важные замечания

1. **НЕ создаем новую физику** - используем существующие методы
2. **НЕ переписываем способности** - вызываем их методы
3. **Используем Fix вместо float** - игра использует fixed-point математику
4. **Harmony Prefix возвращает false** - блокирует оригинальный метод
5. **Traverse для приватных полей** - безопасный доступ через рефлексию

## Расширение

Чтобы добавить новую синергию:

1. Добавить enum в `SynergyType`
2. Добавить проверку в `GetSynergyType()`
3. Создать новый класс `XxxYyySynergy.cs`
4. Добавить case в `ActivateSynergy()`
5. Реализовать метод `Activate()` используя существующие компоненты игры
