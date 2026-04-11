# 🎮 FumoSnake - Курсовой Проект

**Разработчик:** solo  
**Технология:** MonoGame + C#  
**Тип:** 2D игра  
**GitHub:** https://github.com/kiralizanchuk/monogame-fumosnake

---

## 📋 Описание Проекта

Расширенная версия Flappy Bird на MonoGame с **4 полноценными фичами** для повышения интерактивности и переиграемости.

## ✨ Планируемые Фичи

### 1️⃣ **Разные режимы игры** (обязательно)
- **Classic Mode** - стандартный Flappy Bird (бесконечно)
- **Time Attack** - 30 секунд максимум очков
- **Endless** - растущая сложность (скорость + препятствия)
- **Menu для выбора режима**

### 2️⃣ **Система прогрессии** (обязательно)
- Уровни сложности (1-10)
- На каждом уровне: +10% скорость, -5% gap между трубами
- Разблокировка цветов для игрока
- Сохранение best score для каждого режима

### 3️⃣ **Powerup система** (обязательно)
- **Shield** (+1 жизнь, спасает от удара один раз)
- **Slow Motion** (замедление в 2x на 5 сек)
- **Double Score** (×2 очки на 10 сек)
- Спавн случайно, редко (1 раз в 20-30 сек)

### 4️⃣ **Полноценный UI + Audio**
- Главное меню (выбор режима, статистика, настройки)
- Pause меню (Resume, Menu, Exit)
- In-game HUD (счет, уровень, powerups)
- Background музыка + звуки (прыжок, удар, очко, powerup)
- Settings (включение звука)

---

## 🏗️ Архитектура

```
FumoGame/
├── Program.cs                 # Entry point
├── Managers/
│   ├── GameStateManager.cs    # Menu, Playing, Pause, GameOver
│   ├── GameModeManager.cs     # Classic, TimeAttack, Endless
│   ├── AudioManager.cs        # Музыка и звуки
│   ├── UIManager.cs           # Рендер меню и HUD
│   └── SaveManager.cs         # Сохранение scores
├── Entities/
│   ├── Player.cs              # Игрок с жизнями
│   ├── Pipe.cs                # Препятствия
│   └── Powerup.cs             # Система powerup
├── Tests/
│   ├── PlayerTests.cs         # Тесты столкновений
│   ├── PipeTests.cs           # Тесты логики труб
│   └── GameModeTests.cs       # Тесты режимов
└── Content/                   # Спрайты, звуки
```

---

## 🔧 Технические детали

### Game States
- **Menu** - выбор режима, статистика
- **Playing** - основной geimplay
- **Pause** - пауза
- **GameOver** - экран смерти
- **LevelUp** - переход на новый уровень

### Powerup Mechanics
- Спавн: случайно в промежутках между трубами
- Визуал: разные цвета, мерцание
- Срок действия: отсчёт на HUD
- Стакование: нельзя, новый powerup заменяет старый

### Scoring System
- **Classic:** +1 очко за трубу
- **Time Attack:** +1 очко за трубу (максимум 30 сек)
- **Endless:** +1 очко × (уровень × 2) за трубу

---

## 📊 Unit Tests

```
Tests/
├── CollisionTests - столкновения игрока и труб
├── ScoringTests - система очков разных режимов  
├── PowerupTests - применение powerup
├── LevelProgressionTests - система уровней
└── SaveDataTests - сохранение/загрузка
```

Минимум **3-4 теста на каждый компонент**.

---

## 📅 План Разработки

### Неделя 1: Архитектура + Game States
- Рефактор текущего кода
- Система Game States
- Menu с выбором режима

### Неделя 2: Режимы игры + Progression
- Implements GameModes
- Система уровней
- SaveManager

### Неделя 3: Powerups + Audio
- Powerup система
- AudioManager
- Звуки

### Неделя 4: UI Polish + Tests
- UI итерация
- Unit Tests
- Bug fixes
- Финальная сборка

---

## 🎨 Визуалы

- Спрайты: генерируются программно (пиксель-арты, как текущий)
- UI: текст на встроенном шрифте
- Эффекты: Fade, wobble при powerup, частицы

---

## 📝 Критерии Оценки

✅ **Модульность кода** - GameStateManager, ModeManager  
✅ **Фичи реализованы** - 4 фичи полностью  
✅ **Unit tests** - минимум 12+ тестов  
✅ **Visuals** - приятный интерфейс, читаемый UI  
✅ **Code quality** - следование SOLID, читаемость  

---

**Статус:** ✏️ В разработке  
**Последнее обновление:** 11.04.2026
