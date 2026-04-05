# 📥 Установка Bopl Battle Synergy Mod

## Требования

- ✅ **Bopl Battle** (Steam версия)
- ✅ **BepInEx 5.4.21+** (уже установлен)

## 🚀 Быстрая установка

### Шаг 1: Скачать мод

**Вариант A: Из релизов (рекомендуется)**
```bash
# Скачайте BoplSynergyMod.dll из последнего релиза
# https://github.com/im-blatnoyua/BoplSynergyMod/releases
```

**Вариант B: Собрать самому**
```bash
cd /home/blatnoy/BoplSynergyMod
dotnet build -c Release
# DLL будет в bin/Release/net471/BoplSynergyMod.dll
```

### Шаг 2: Установить в игру

```bash
# Скопировать DLL в папку плагинов BepInEx
cp BoplSynergyMod.dll ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/

# Или если игра в другом месте:
cp BoplSynergyMod.dll /path/to/Bopl\ Battle/BepInEx/plugins/
```

### Шаг 3: Запустить игру

```bash
# Запустите игру через Steam
steam steam://rungameid/1686940

# Или напрямую
~/.steam/steam/steamapps/common/Bopl\ Battle/BoplBattle.x86_64
```

## 📁 Структура папок

После установки должно быть так:

```
Bopl Battle/
├── BoplBattle.x86_64
├── BoplBattle_Data/
├── BepInEx/
│   ├── core/
│   ├── plugins/
│   │   └── BoplSynergyMod.dll    ← ВАШ МОД ЗДЕСЬ
│   ├── config/
│   └── LogOutput.log
└── ...
```

## ✅ Проверка установки

### 1. Проверить логи BepInEx

```bash
# Открыть лог файл
tail -f ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log

# Искать строки:
# [Info   : BepInEx] Loading [BoplSynergyMod 1.0.0]
# [Info   :BoplSynergyMod] Loading v1.0.0...
# [Info   :BoplSynergyMod] All patches applied successfully.
# [Info   :BoplSynergyMod] Loaded successfully!
```

### 2. Проверить в игре

1. Запустите игру
2. Выберите персонажа с двумя способностями (например: Луч + Размножение)
3. Одновременно нажмите обе кнопки способностей
4. Должна активироваться синергия!

## 🎮 Как использовать

### Синергия 1: Луч + Размножение
- **Кнопки**: Нажмите кнопку луча + кнопку размножения одновременно
- **Эффект**: Стреляет 3 лучами (центр, +45°, -45°)
- **Бонус**: Отталкивает игрока назад

### Синергия 2: Луч + Увеличение
- **Кнопки**: Нажмите кнопку луча + кнопку увеличения одновременно
- **Эффект**: Луч увеличивает объекты, игрок уменьшается
- **Ограничение**: Игрок не может стать меньше 0.3

### Синергия 3: Луч + Перемещение
- **Кнопки**: Нажмите кнопку луча + кнопку магнита одновременно
- **Эффект**: Луч притягивает объекты к игроку

## 🔧 Устранение неполадок

### Мод не загружается

**Проблема**: В логах нет упоминания BoplSynergyMod

**Решение**:
```bash
# 1. Проверить что BepInEx установлен
ls ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/

# 2. Проверить что DLL в правильной папке
ls ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/BoplSynergyMod.dll

# 3. Проверить права доступа
chmod +r ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/BoplSynergyMod.dll
```

### Синергии не активируются

**Проблема**: Нажимаю кнопки, но ничего не происходит

**Решение**:
1. Убедитесь что нажимаете кнопки **одновременно**
2. Проверьте что у вас есть обе нужные способности
3. Проверьте логи на наличие ошибок:
```bash
grep -i "synergy\|error" ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log
```

### Игра крашится

**Проблема**: Игра вылетает при запуске

**Решение**:
```bash
# 1. Удалить мод временно
rm ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/BoplSynergyMod.dll

# 2. Запустить игру и проверить что она работает

# 3. Проверить версию BepInEx
cat ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/core/BepInEx.dll
# Должна быть 5.4.21 или новее

# 4. Переустановить мод
```

## 🗑️ Удаление мода

```bash
# Просто удалить DLL
rm ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/BoplSynergyMod.dll

# Игра вернется к обычному поведению
```

## 🔄 Обновление мода

```bash
# 1. Удалить старую версию
rm ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/BoplSynergyMod.dll

# 2. Скачать новую версию
# https://github.com/im-blatnoyua/BoplSynergyMod/releases

# 3. Скопировать новую DLL
cp BoplSynergyMod.dll ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/
```

## 📝 Логи для отладки

```bash
# Смотреть логи в реальном времени
tail -f ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log

# Искать ошибки
grep -i "error\|exception" ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log

# Искать сообщения мода
grep "BoplSynergyMod\|Synergy" ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log
```

## 🎯 Полезные команды

```bash
# Найти путь к игре
find ~/.steam -name "BoplBattle.x86_64" 2>/dev/null

# Проверить установленные моды
ls -lh ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/plugins/

# Очистить логи
> ~/.steam/steam/steamapps/common/Bopl\ Battle/BepInEx/LogOutput.log

# Запустить игру с логами в консоли
~/.steam/steam/steamapps/common/Bopl\ Battle/BoplBattle.x86_64 2>&1 | tee game.log
```

## 🆘 Поддержка

Если возникли проблемы:

1. **Проверьте логи** - большинство проблем видны в логах
2. **GitHub Issues** - https://github.com/im-blatnoyua/BoplSynergyMod/issues
3. **Приложите логи** - скопируйте содержимое LogOutput.log

---

**Готово! Наслаждайтесь синергиями! 🎮**
