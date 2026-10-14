# Annotate

For issues, pull requests, and when a change also needs specs, journeys, wiki, or other docs updated, start with [docs/contributing/README.md](docs/contributing/README.md).

To work on an issue, run `gh issue develop <number> -c`. That creates the linked branch and checks it out. Do not create, check out, or push another branch for that work.

Do not merge a pull request. Do not run `gh pr merge`, with or without `--squash` or `--admin`, and do not merge through `gh api`, `curl`, or `mergePullRequest`. Do not approve a pull request. Do not push `main` or `master`. A person squash-merges. `.cursor/cli.json` denies those commands, and `.cursor/permissions.json` tells Cursor to block them.

Read [docs/design.md](docs/design.md) and [docs/vocabulary.md](docs/vocabulary.md) before changing the code. Read [docs/ui.md](docs/ui.md) before changing `Components/` or `wwwroot/css/`: tokens come from `tokens.css`, buttons, badges, dialogs, context blocks, and empty states come from `Components/Ui/`, each action lives in exactly one place, and `data-*` attributes are the test and JavaScript contract.

Annotate.Markdown is the shared project listed in `build/shared-projects.txt`. A feature may reference a shared project. Features never reference each other. Annotate.Web is the composition root and the only project that wires features.

The allowed project references are the table in `docs/design.md`.

Write a failing test before the behaviour it describes. Keep each `.cs` and `.razor` file under `src/` and `tests/` to 500 physical lines. Ignore `bin`, `obj`, `Migrations`, and `*.g.cs`.

Run `build/check.sh` and leave it green before handing work back.
