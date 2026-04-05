# Bopl Battle Synergy Mod - Краткое руководство

## ✅ Что реализовано

Мод успешно собран и готов к использованию! Реализованы 3 синергии:

### 1. **Луч + Размножение** (Beam + Duplicate)
- Одновременно нажмите кнопки луча и размножения
- Стреляет 3 лучами под углами (центр, +45°, -45°)
- Отталкивает игрока назад (recoil)

### 2. **Луч + Увеличение** (Beam + Grow)
- Одновременно нажмите кнопки луча и увеличения
- Луч увеличивает объекты
- Игрок уменьшается с каждым выстрелом
- Останавливается при минимальном размере (0.3)

### 3. **Луч + Перемещение** (Beam + Telekinesis/Magnet)
- Одновременно нажмите кнопки луча и магнита
- Луч притягивает объекты к игроку
- Постепенное притягивание через физику

## 📦 Установка

1. Убедитесь что установлен **BepInEx 5.4.21+**
2. Скопируйте `BoplSynergyMod.dll` из `bin/Release/net471/` в папку `BepInEx/plugins/`
3. Запустите игру

## 🔧 Архитектура

### Как это работает

1. **Перехват input**: Harmony патч на `SlimeController.OldUpdate()` перехватывает нажатия кнопок
2. **Определение синергии**: Проверяет имена способностей и находит комбинации
3. **Активация**: Вызывает существующие методы игры (НЕ переписывает логику)
4. **Кулдауны**: Устанавливает кулдауны обеих способностей

### Ключевые принципы

- ✅ **Максимальное переиспользование** существующего кода игры
- ✅ **НЕ создаем новую физику** - используем существующие методы
- ✅ **НЕ переписываем способности** - вызываем их методы
- ✅ **Harmony Prefix** блокирует стандартное поведение при синергии
- ✅ **Traverse** для доступа к приватным полям

### Используемые классы игры

```csharp
// Input
Player.AbilityButtonIsDown(int index)
Player.InputProfile.firstAbilityButton_IS_DOWN

// Способности
SlimeController.abilities
AbilityMonoBehaviour.EnterAbility()

// Стрельба
IGun.Shoot(Vec2, Vec2, ref bool, int, bool)
ShootScaleChange.Shoot()

// Физика
DetPhysics.RaycastToClosest()
BoplBody.velocity
PlayerBody.selfImposedVelocity
```

## 📁 Структура проекта

```
BoplSynergyMod/
├── Plugin.cs                      # Точка входа BepInEx
├── Patches/
│   └── AbilityInputPatch.cs      # Перехват нажатий кнопок
├── Synergies/
│   ├── SynergyDefinition.cs      # Определения и трекер
│   ├── BeamDuplicateSynergy.cs   # Луч + Размножение
│   ├── BeamGrowSynergy.cs        # Луч + Увеличение
│   └── BeamTelekinesisSynergy.cs # Луч + Перемещение
├── libs/                          # Библиотеки игры
└── Decompiled/                    # Декомпилированный код (справка)
```

## 🚀 Сборка

```bash
cd /home/blatnoy/BoplSynergyMod
dotnet build -c Release
```

DLL будет в `bin/Release/net471/BoplSynergyMod.dll`

## 🔍 Как добавить новую синергию

1. Добавьте enum в `SynergyType` (SynergyDefinition.cs)
2. Добавьте проверку в `GetSynergyType()` (AbilityInputPatch.cs)
3. Создайте новый класс `XxxYyySynergy.cs`
4. Добавьте case в `ActivateSynergy()` (AbilityInputPatch.cs)
5. Реализуйте метод `Activate()` используя существующие компоненты

## 📖 Подробная документация

См. [ARCHITECTURE.md](ARCHITECTURE.md) для детального описания архитектуры и реализации.

## ⚠️ Важные замечания

- Игра использует **Fix** (fixed-point) математику, не float
- Все векторы - **Vec2** из BoplFixedMath
- Harmony Prefix возвращает **false** чтобы заблокировать оригинальный метод
- Используйте **Traverse** для доступа к приватным полям
- Определение способностей по имени GameObject (`.Contains("beam")`)

## 🎮 Тестирование

1. Запустите игру с модом
2. Выберите персонажа с нужными способностями
3. Одновременно нажмите две кнопки способностей
4. Проверьте логи BepInEx для отладки

## 📝 Логи

Логи находятся в `BepInEx/LogOutput.log`

Ищите строки с `[BoplSynergyMod]` и `[Synergy]`

---

**Статус**: ✅ Готово к использованию
**Версия**: 1.0.0
**Дата сборки**: 2026-04-05
