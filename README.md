# DailyTasks

A lightweight Windows desktop app for planning your day **and** running
methodology-organized projects. DailyTasks keeps the focus on a small set of
daily priorities, captures new tasks from anywhere with a global hotkey,
understands plain-English due dates and priorities as you type — and, when you
need more structure, turns any task with subtasks into a full SDLC project with
phases, a Gantt chart, a team, and an Excel export.

> **Platform:** Windows only · **Framework:** .NET 10 (WPF)

## Features

### Everyday tasks

- **Today view** — your working list for the day, with a "Big 3" pin that
  highlights up to three priority tasks and expires on its own each morning, a
  stale-task nudge, and a workload warning when your estimates overflow the day.
- **Quick Capture** — press **Ctrl + Shift + T** anywhere in Windows to pop up a
  capture box, jot a task, and get back to what you were doing.
- **Natural-language parsing** — type `Call dentist tomorrow 3pm high priority`
  and the due date, time, and priority are pulled out automatically, leaving a
  clean task title. Supports `today`, `tonight`, `tomorrow`, `next week`,
  weekdays, `in 3 days`, `14 Jul`, `7/14`, `noon`/`midnight`, `!high`, and more.
- **Categories** — organise tasks into colour-coded groups (Work, Personal,
  Errands out of the box).
- **Recurring tasks** — daily/weekly/monthly tasks spawn their next occurrence
  when you complete them.
- **All Tasks / Completed / Insights** — browse everything, review what's done,
  and see progress over time.
- **Calendar** — a month grid of every dated task and activity, so nothing slips.
- **Focus sessions** — a built-in Pomodoro timer, plus a daily end-of-day recap.

### Projects & methodologies

Tasks and projects are kept deliberately distinct. A plain task with subtasks
can be **organized** into a project by picking a software-development
methodology; projects then live on their own **Projects** page (never mixed in
with your to-dos). Twelve methodologies are supported, each with a structurally
correct visualization — you never pick the chart type, it follows the
methodology:

| Methodology | Structure | Visualization |
| --- | --- | --- |
| Waterfall | Sequential locked phases | Sequential Gantt |
| V-Model | Paired dev ↔ test phases | V-shaped Gantt |
| Spiral / Iterative & Incremental / RAD | Repeating cycles | Cyclical Gantt |
| Agile / Scrum / XP | Backlog + sprints | **Agile Gantt** (sprint-grouped rows) |
| Kanban / Lean | Status columns (Lean adds WIP limits) | Drag-and-drop board |
| DevOps | Continuous stages | Looping pipeline diagram |
| Big Bang | No structure | Flat task list |

- **Agile Gantt** — a real row-based Gantt grouped by sprint: Sprint / Activity /
  Assigned / Start / End / Duration / Status / **% Done** on the left, a dated
  calendar timeline of status-coloured, %-filled bars on the right. The **% Done**
  cell is editable inline and recolours the bar.
- **Per-project teams** — each project has its own team. Add members from the
  project's detail window; the subtask assignee picker is scoped to that project.
- **Excel export** — export a project's SDLC to a modern `.xlsx` workbook: a data
  sheet plus a Gantt worksheet (Sprint/Activity/Assigned/Start/End/Duration/
  Status/% Done with a colour-coded week calendar), adapted per methodology.

### Developer features (optional)

Enable **developer features** in Settings to unlock git integration:

- **Git links** — tag a task with a marker (e.g. `TASK-42`, `closes #42`).
- **Per-project repositories** — point each project at a local git repo. Its
  detail window's **Commits** panel shows every recent commit, and the watcher
  auto-completes any subtask whose git link appears in a newer commit. A global
  repository in Settings still applies to every linked task, and a **Check now**
  button scans on demand.

### Everything else

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
| Excel export       | [ClosedXML](https://github.com/ClosedXML/ClosedXML) |
| DI                 | Microsoft.Extensions.DependencyInjection         |

## Project layout

```
DailyTasks/
├── Data/           EF Core DbContext, migrations, design-time factory
├── Models/         Entities (TaskItem, Category, Phase, TeamMember) and enums
├── Services/       Task/team/category services, settings, git watcher, Excel
│                   export, text parser, Gantt scheduling
├── ViewModels/     MVVM view models for each page, window, and chart
└── Views/
    ├── Pages/          Navigation pages (Today, All Tasks, Projects, Calendar,
    │                   Gantt, Insights, Completed, Settings)
    ├── Charts/         Gantt / pipeline chart controls
    ├── Coordinators/   UI-service implementations (task/team coordinators,
    │                   editors, Excel exporter)
    └── …               Windows, shared controls, resource dictionaries, converters
```

## Requirements

- **Windows 10/11**
- **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (to build) — the
  published app needs only the .NET 10 Desktop Runtime.
- **git** on your `PATH` — optional, only for the developer git-integration
  features.

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

Your tasks are stored in a local SQLite database, and your preferences in a JSON
file, both under:

```
%LOCALAPPDATA%\DailyTasks\dailytasks.db
%LOCALAPPDATA%\DailyTasks\settings.json
```

To reset the app to a clean state, close it and delete those files — a fresh
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
