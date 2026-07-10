# DailyTasks

A lightweight Windows desktop to-do app for planning your day. DailyTasks keeps
the focus on a small set of daily priorities, captures new tasks from anywhere
with a global hotkey, and understands plain-English due dates and priorities as
you type.

> **Platform:** Windows only · **Framework:** .NET 10 (WPF)

## Features

- **Today view** — your working list for the day, with a "Big 3" pin that
  highlights up to three priority tasks and expires on its own each morning.
- **Quick Capture** — press **Ctrl + Shift + T** anywhere in Windows to pop up a
  capture box, jot a task, and get back to what you were doing.
- **Natural-language parsing** — type `Call dentist tomorrow 3pm high priority`
  and the due date, time, and priority are pulled out automatically, leaving a
  clean task title. Supports `today`, `tonight`, `tomorrow`, `next week`,
  weekdays, `in 3 days`, `14 Jul`, `7/14`, `noon`/`midnight`, `!high`, and more.
- **Categories** — organise tasks into colour-coded groups (Work, Personal,
  Errands out of the box).
- **All Tasks / Completed / Insights** — browse everything, review what's done,
  and see progress over time.
- **Light / dark themes** via [WPF-UI](https://github.com/lepoco/wpfui).
- **Local-first storage** — everything lives in a local SQLite database; no
  account, no cloud, no network required.

## Tech stack

| Concern            | Choice                                           |
| ------------------ | ------------------------------------------------ |
| UI                 | WPF on .NET 10 (`net10.0-windows`)               |
| MVVM               | [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| UI toolkit / theme | [WPF-UI](https://github.com/lepoco/wpfui)        |
| Data access        | Entity Framework Core + SQLite                   |
| DI                 | Microsoft.Extensions.DependencyInjection         |

## Project layout

```
DailyTasks/
├── Data/           EF Core DbContext, migrations, design-time factory
├── Models/         Entities (TaskItem, Category) and enums
├── Services/       Task/category services, settings, global hotkey, text parser
├── ViewModels/     MVVM view models for each page and window
└── Views/          XAML windows, pages, controls, and value converters
```

## Requirements

- **Windows 10/11**
- **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (to build) — the
  published app needs only the .NET 10 Desktop Runtime.

## Installation

### 1. Clone

```powershell
git clone <your-repo-url>
cd TODO-DESKTOP-APPLICATION
```

### 2. Restore & build

```powershell
dotnet restore
dotnet build
```

### 3. Run

```powershell
dotnet run --project DailyTasks
```

The database and its schema are created automatically on first launch — EF Core
migrations run at startup, so there's no separate setup step.

### 4. (Optional) Publish a standalone build

```powershell
dotnet publish DailyTasks -c Release -r win-x64 --self-contained false -o publish
```

The runnable app is then in the `publish/` folder (`DailyTasks.exe`).

## Data storage

Your tasks are stored in a local SQLite database at:

```
%LOCALAPPDATA%\DailyTasks\dailytasks.db
```

To reset the app to a clean state, close it and delete that file — a fresh
database is recreated on the next launch.

## Development

The solution uses the newer `.slnx` format:

```powershell
dotnet build DailyTasks.slnx
```

### Working with the database schema

EF Core migrations live in `DailyTasks/Data/Migrations`. To add one after
changing the model:

```powershell
dotnet ef migrations add <MigrationName> --project DailyTasks
```

Migrations are applied automatically at startup, so you normally don't need to
run `dotnet ef database update` by hand.
