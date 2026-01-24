---
name: WPF CIDR IP Manager (Multi-Project)
overview: "Создание .NET решения с разделением на проекты: библиотека с логикой работы с CIDR, проект тестов и отдельный WPF UI проект"
todos:
  - id: create-solution
    content: Создать решение .NET и структуру трех проектов (Core, Core.Tests, UI)
    status: pending
  - id: setup-dependencies
    content: "Настроить зависимости: UI -> Core, Tests -> Core"
    status: pending
  - id: setup-core-project
    content: Настроить Core проект (Class Library) с NuGet пакетами (YamlDotNet)
    status: pending
  - id: setup-tests-project
    content: Настроить проект тестов с фреймворком тестирования (xUnit/MSTest)
    status: pending
  - id: setup-ui-project
    content: Настроить WPF UI проект с зависимостью от Core
    status: pending
  - id: create-models
    content: Создать модель IpRange в Core проекте
    status: pending
  - id: create-cidr-service
    content: Реализовать CidrService в Core с парсингом CIDR, проверкой пересечений и умным объединением
    status: pending
  - id: create-yaml-service
    content: Реализовать YamlStorageService в Core для сохранения/загрузки данных в YAML
    status: pending
  - id: write-core-tests
    content: Написать unit-тесты для CidrService и YamlStorageService
    status: pending
  - id: create-ui
    content: Создать WPF UI с полями ввода, списком адресов и кнопками экспорта
    status: pending
  - id: implement-ui-logic
    content: Реализовать логику в UI проекте с использованием Core библиотеки
    status: pending
---

# WPF приложение для управления IP адресами в CIDR-нотации

## Архитектура решения

Решение будет состоять из трех проектов:

1. **VpnIpTable.Core** - библиотека классов с основной логикой (модели, сервисы, CIDR-операции)
2. **VpnIpTable.Core.Tests** - проект с unit-тестами для Core библиотеки
3. **VpnIpTable.UI** - WPF приложение для пользовательского интерфейса

## Структура проекта

### VpnIpTable.Core (Class Library)

**Модели:**

- `Models/IpRange.cs` - класс для представления CIDR-адреса

**Сервисы:**

- `Services/CidrService.cs` - логика работы с CIDR (парсинг, валидация, проверка пересечений, умное объединение)
- `Services/YamlStorageService.cs` - сервис для работы с YAML файлом (чтение/запись)

**Публичный API:**

- Методы для парсинга CIDR
- Методы для проверки вхождения адресов/подсетей
- Методы для умного объединения (удаление подмножеств)
- Методы для экспорта в разных форматах (CSV-подобный, Route команды)
- Методы для сохранения/загрузки из YAML

### VpnIpTable.Core.Tests (xUnit/MSTest Test Project)

**Тесты:**

- Unit-тесты для `CidrService` (парсинг, пересечения, объединение)
- Unit-тесты для `YamlStorageService` (сохранение/загрузка)
- Интеграционные тесты для основных сценариев

### VpnIpTable.UI (WPF Application)

**UI компоненты:**

- `App.xaml` / `App.xaml.cs` - точка входа приложения
- `MainWindow.xaml` / `MainWindow.xaml.cs` - главное окно
- Текстовое поле для ввода адресов (многострочное)
- Список адресов (ListBox/DataGrid)
- Кнопки: Загрузить из файла, Добавить, Удалить, Экспорт (CSV), Экспорт (Route)
- Автоматическое сохранение при добавлении/удалении адресов
- Автоматическая загрузка данных при старте приложения
- Извлечение адресов из "грязных" данных (текст с адресами) с помощью регулярных выражений
- Загрузка текста в поле ввода из файла

**Зависимости:**

- Ссылка на проект `VpnIpTable.Core`

## Основная логика

### CIDR-операции (в Core)

- Парсинг CIDR-нотации (например, "192.168.1.0/24")
- Парсинг IP-адресов без маски (например, "192.168.0.1") - интерпретируются как /32 (один адрес)
- Валидация корректности CIDR формата и IP-адресов
- Извлечение адресов из текста с помощью регулярных выражений (игнорирование "грязных" данных)
- Проверка вхождения одного адреса/подсети в другой
- Умное объединение: при добавлении новой записи автоматически удалять все подмножества
- Формирование маски подсети для команды route ADD
- Экспорт в формате CSV (одна строка, разделитель запятая)
- Экспорт в формате Route команд (многострочный)

### Хранение данных (в Core)

- YAML файл (по умолчанию `addresses.yaml`)
- Структура: список строк в формате CIDR
- Автоматическое сохранение при добавлении/удалении адресов
- Автоматическая загрузка данных при старте приложения

## Технические детали

### NuGet пакеты

**VpnIpTable.Core:**

- `YamlDotNet` - для работы с YAML

**VpnIpTable.Core.Tests:**

- `xunit` или `MSTest` - фреймворк тестирования
- `Moq` или `NSubstitute` (при необходимости для моков)

**VpnIpTable.UI:**

- Стандартные библиотеки WPF
- Ссылка на проект `VpnIpTable.Core`

### Работа с CIDR

- Использование `System.Net.IPAddress` (встроен в .NET)
- Собственная реализация логики работы с CIDR (так как `System.Net.IPNetwork` доступен только в .NET 5+)
- Для расчета маски подсети: вычисление на основе префикса CIDR

## Структура файлов

```
VpnIpTable/
├── VpnIpTable.sln                    # Решение
├── VpnIpTable.Core/
│   ├── VpnIpTable.Core.csproj
│   ├── Models/
│   │   └── IpRange.cs
│   ├── Services/
│   │   ├── CidrService.cs
│   │   └── YamlStorageService.cs
│   └── VpnIpTable.Core.csproj
├── VpnIpTable.Core.Tests/
│   ├── VpnIpTable.Core.Tests.csproj
│   ├── Services/
│   │   ├── CidrServiceTests.cs
│   │   └── YamlStorageServiceTests.cs
│   └── ...
└── VpnIpTable.UI/
    ├── VpnIpTable.UI.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── MainWindow.xaml
    └── MainWindow.xaml.cs
```

## Реализация

1. Создание структуры решения с тремя проектами
2. Настройка зависимостей между проектами
3. Добавление NuGet пакетов в соответствующие проекты
4. Реализация Core библиотеки (модели, сервисы)
5. Написание unit-тестов для Core
6. Создание WPF UI приложения
7. Интеграция UI с Core библиотекой
8. Тестирование всего решения
