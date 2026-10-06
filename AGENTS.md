# Annotate

Read [docs/design.md](docs/design.md) and [docs/vocabulary.md](docs/vocabulary.md) before changing the code. Read [docs/ui.md](docs/ui.md) before changing `Components/` or `wwwroot/css/`: tokens come from `tokens.css`, buttons, badges, dialogs, context blocks, and empty states come from `Components/Ui/`, each action lives in exactly one place, and `data-*` attributes are the test and JavaScript contract.

Annotate.Markdown is the shared project listed in `build/shared-projects.txt`. A feature may reference a shared project. Features never reference each other. Annotate.Web is the composition root and the only project that wires features.

The allowed project references are the table in `docs/design.md`.

Write a failing test before the behaviour it describes. Keep each `.cs` and `.razor` file under `src/` and `tests/` to 500 physical lines. Ignore `bin`, `obj`, `Migrations`, and `*.g.cs`.

Run `build/check.sh` and leave it green before handing work back.
