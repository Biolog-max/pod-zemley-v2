# «Под землёй» — Карта проекта

## Что это за игра
Мобильная игра для уроков истории (10 класс). Игрок — музейный хранитель.
Получает ящик с артефактами, осматривает каждый 4 инструментами (линейка, весы,
¹⁴C-датировка, словарь), сверяется со справочником аналогов и размещает на полку
в нужном зале музея. За правильное размещение — очки и ранг.

**Unity 6000.2.8f1 · Portrait 1080×1920 · ScreenSpaceOverlay · UI.Text (не TMP)**

---

## Как устроен проект

Весь UI строится **кодом** (не в редакторе Unity). Одна сцена `SampleScene.unity`
содержит только два объекта: `GameState` и `ScreenManager`. При запуске
`ScreenManager` создаёт Canvas, регистрирует все экраны, и показывает меню.

Переходы между экранами: `GameState` вызывает событие `OnScreen`,
`ScreenManager` прячет все экраны и показывает нужный.

---

## Скрипты

### Core/ — ядро (НЕ ТРОГАТЬ без согласования)

| Файл | Строк | Что делает |
|------|-------|------------|
| **GameState.cs** | 187 | Синглтон — хранит всё состояние игры: текущий экран, артефакт, таймер, очки, использованные инструменты, размещения. Содержит методы переходов (`NewRound`, `BeginExam`, `GoMuseum`, `ConfirmPlace`, `NextArt` и т.д.) и подсчёт очков/ранга. Все экраны читают из него и вызывают его методы. |
| **ArtifactData.cs** | 75 | Модели данных (классы): `TraitSet` (5 характеристик артефакта), `ZoneData` (зона осмотра), `ArtifactInfo` (артефакт), `HallData`, `WallData`, `ToolData`, `ScoringData`, `ConfigData`, `RoundData`, `Placement`. Все `[Serializable]` для JSON. |
| **DataLoader.cs** | 35 | Загрузка JSON из `Resources/Config/` (Newtonsoft.Json) и спрайтов из `Resources/Art/`. Методы: `LoadConfig()`, `LoadCatalog()`, `LoadRound(id)`, `LoadSprite(path)`. |
| **CatalogDB.cs** | 32 | Поиск похожих артефактов в каталоге. Метод `FindSimilar(artifact, revealed)` — сравнивает по раскрытым характеристикам, возвращает список с % совпадения. Используется в Справочнике. |
| **BaseScreen.cs** | 26 | Абстрактный базовый класс экрана. Создаёт панель (`UIKit.Panel`), хранит ссылку на `GameState`. Методы: `Init()`, `Show()`, `Hide()`, абстрактные `Build()` и `Refresh()`. |
| **ScreenManager.cs** | 79 | MonoBehaviour на сцене. При `Start()` создаёт Canvas (1080×1920, ScaleWithScreenSize), EventSystem, регистрирует все 9 экранов. Слушает `OnScreen` — прячет все, показывает нужный. Автопереход через 2 сек после размещения. В `Update()` обновляет таймер. |
| **UIKit.cs** | 127 | Статические хелперы для построения UI кодом: `Txt()` — текст, `Btn()` — кнопка, `Img()` — прямоугольник, `SprImg()` — спрайт, `Pos()` — позиция от верха, `PosBot()` — позиция от низа, `Panel()` — полноэкранная панель. Цвета: `BG`, `PRI`, `T1-T3`, `RED`, `ACCENT`. |

### Screens/ — экраны (каждый файл = 1 экран)

| Файл | Строк | Что отображает | Откуда попадаем | Куда ведёт |
|------|-------|---------------|----------------|------------|
| **MenuScreen.cs** | 42 | Заголовок "ПОД ЗЕМЛЁЙ", кнопки: Новая партия / Продолжить (неактив) / Галерея (неактив) / Настройки (неактив). Селектор времени раунда (30/60/90/120/180/300 сек). | Запуск, итоги | → Briefing |
| **BriefingScreen.cs** | 63 | Описание раунда, 4 карточки инструментов с иконками, 3 баннера залов. Кнопка "НАЧАТЬ РАУНД". | Menu | → Examine |
| **ExamineScreen.cs** | 356 | **Главный экран.** "Стол археолога": артефакт 75% экрана, 4 карточки инструментов, блокнот (5 полей), кнопки ◀▶ переворота, зона описания. 3 кнопки внизу: Справочник / Пропустить / В МУЗЕЙ. Содержит: drag & drop инструментов, 4 процедурные анимации (линейка, весы, ¹⁴C, словарь с лупой и inscription), flip-анимацию сторон. Классы `ToolCardDrag` и `ToolDropZone` тоже здесь. | Briefing, Reference, Placed (следующий арт) | → Reference, Museum |
| **ReferenceScreen.cs** | 75 | Справочник аналогов. 3 карточки на странице с миниатюрами, характеристиками, залом/полкой и % совпадения. Пагинация. | Examine | → Examine |
| **MuseumScreen.cs** | 139 | Два экрана внутри: (1) Выбор зала — 3 баннера с описаниями, (2) Выбор полки — 3 полки с деревянными линиями, карточка артефакта внизу. Тап по полке = разместить. | Examine | → Confirm |
| **ConfirmScreen.cs** | 40 | Модальное окно поверх затемнения. Показывает: артефакт, выбранный зал, полку. "Переставить нельзя!". Кнопки: Отмена / Да. | Museum | → Placed или Museum |
| **PlacedScreen.cs** | 39 | Галочка + "Размещён!" (или "Пропущен"), зал и полка, сколько осталось, время. Через 2 сек автопереход к следующему артефакту. | Confirm, Skip | → Examine (след. арт) или Results |
| **ResultsScreen.cs** | 69 | Таблица всех артефактов (зал ✓/✗, полка ✓/✗, очки). Итого + время. Ранг (Практикант → Директор музея). Случайный факт. Кнопки: В меню / Ещё раунд. | Последний PlacedScreen, TimeUp | → Menu, Briefing |
| **TimeUpScreen.cs** | 22 | Модальное окно "Время вышло!" с иконкой часов. Кнопка "Результаты →". | Таймер дошёл до 0 | → Results |

### Editor/

| Файл | Строк | Что делает |
|------|-------|------------|
| **TestHelper.cs** | 92 | 15 пунктов меню Unity `Test/*` для быстрого тестирования: Screenshot, New Round, Begin Exam, Apply каждого инструмента, Go Museum, Place, Confirm, Skip, Next, Print State, Full Flow (прогоняет весь раунд за 1 клик с правильными ответами → 1080 очков). |

---

## Данные (JSON)

Все файлы в `Assets/Resources/Config/`.

| Файл | Что внутри |
|------|------------|
| **config.json** | 3 зала (`hall_russia_19`, `hall_europe_20`, `hall_ussr_20`), 3 типа полок (`weapons`, `household`, `documents`), 4 инструмента (`ruler`→size, `carbon`→year, `scales`→weight, `dictionary`→textLanguage), таблица очков. |
| **catalog.json** | Массив артефактов-аналогов для справочника. Каждый: id, name, traits, correctHall, correctWall. Используется в `CatalogDB.FindSimilar()`. |
| **round_01.json** | Раунд "Ящик со стройки": 4 артефакта (монета, ложка, крест, штык), 60 сек, difficulty 1. Каждый артефакт: id, name, traits, zones (стороны с описаниями), correctHall, correctWall, funFact, inscription. |
| **theme.json** | Цветовая тема (пока не используется в коде). |

### Формат артефакта (в round_XX.json)
```json
{
  "id": "coin_1kop_1812",
  "name": "Монета 1 копейка",
  "traits": { "material": "медь", "weight": 6.8, "size": "26 мм", "year": 1812, "textLanguage": "русский" },
  "zones": [
    { "id": "obverse", "name": "Аверс", "image": "coin_1kop_1812_obverse", "description": "Двуглавый орёл...", "revealsTraits": ["material"] },
    { "id": "reverse", "name": "Реверс", "image": "coin_1kop_1812_reverse", "description": "1 КОПѢЙКА 1812...", "revealsTraits": ["year","textLanguage"] }
  ],
  "correctHall": "hall_russia_19",
  "correctWall": "household",
  "funFact": "...",
  "inscription": "coin_1kop_1812_inscription.jpg"
}
```

---

## Графика (90 PNG)

Все в `Assets/Resources/Art/`. Загружаются через `DataLoader.LoadSprite("Art/...")`.

```
Art/
  Artifacts/
    Examine/       — основные изображения артефактов (800×600)
                     coin_1kop_1812_main.png, spoon_soldier_1812_main.png, ...
                     Всего: 10 файлов (4 из round_01 + 6 заготовок для round_02)

    Sides/         — стороны для переворота (800×600)
                     coin_1kop_1812_obverse.png, coin_1kop_1812_reverse.png, ...
                     Всего: 20 файлов (по 2 стороны на артефакт)

    Thumbs/        — миниатюры для справочника и музея (256×256)
                     coin_1kop_1812_thumb.png, ...
                     Всего: 10 файлов

    Inscriptions/  — фото надписей для словаря (крупный план)
                     coin_1kop_1812_inscription.jpg
                     Всего: 1 файл (пока только монета)

  Halls/           — баннеры залов музея (1000×300)
                     hall_russia_19.png, hall_europe_20.png, hall_ussr_20.png

  Icons/           — иконки UI (128×128)
                     tool_ruler.png, tool_carbon.png, tool_scales.png, tool_dictionary.png
                     wall_weapons.png, wall_household.png, wall_documents.png
                     ic_checkmark.png, ic_clock.png, ic_book.png, ic_cross.png, ic_lock.png
                     rank_director.png, rank_intern.png, rank_keeper.png, rank_senior.png
                     + _wide варианты рангов

  Overlays/        — оверлеи инструментов (400×400, пока не используются в коде)
                     overlay_magnifier.png, overlay_ruler.png, overlay_scales.png, overlay_scan.png
```

---

## Сцена

`Assets/Scenes/SampleScene.unity` — единственная сцена. Содержит:
- GameObject `GameState` с компонентом `GameState.cs` + `ScreenManager.cs`
- Camera (по умолчанию, не используется — всё ScreenSpaceOverlay)

**Не редактировать** в Unity Editor — всё создаётся кодом.

---

## Игровой цикл (порядок экранов)

```
Menu → Briefing → Examine ⟷ Reference
                     ↓
                  Museum (зал → полка)
                     ↓
                  Confirm → Placed → [следующий артефакт → Examine]
                                        или
                                     Results → Menu
        TimeUp (при обнулении таймера) → Results
```

---

## Кто может что менять

| Зона | Файлы | Зависит от |
|------|-------|------------|
| **Геймплей** (осмотр, инструменты, анимации) | `ExamineScreen.cs` | GameState (читает/вызывает), UIKit, DataLoader |
| **Музей** (залы, полки, размещение) | `MuseumScreen.cs`, `ConfirmScreen.cs`, `PlacedScreen.cs` | GameState, UIKit, DataLoader |
| **Контент** (артефакты, раунды, картинки) | `Resources/Config/*.json`, `Resources/Art/*` | Формат из ArtifactData.cs |
| **Новые экраны** (галерея, настройки) | новые файлы в `Screens/` + регистрация в `ScreenManager.cs` | BaseScreen, UIKit, GameState |
| **Ядро** (модели, загрузка, переходы) | `Core/*` | ⚠ Все зависят от ядра — менять осторожно |
