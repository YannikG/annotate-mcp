# Development guide

You need the .NET 10 SDK and ast-grep 0.45.3.

```bash
npm install -g @ast-grep/cli@0.45.3
```

```bash
dotnet run --project src/Annotate.Web
```

The site listens on http://127.0.0.1:24173. MCP is at http://127.0.0.1:24173/mcp.

The database file is `annotate.db`. Annotate uses `Annotate:DataDir` when that setting is set, otherwise `$XDG_CONFIG_HOME/annotate`, otherwise `~/.config/annotate`.

Settings:

- `Annotate:DataDir`
- `Annotate:TrustedStoryDomains`
- `Annotate:OpenBrowser`

`Annotate:TrustedStoryDomains` is a list of exact story-link hosts. With no entries, every story URL is refused. A host matches that hostname only, so `docs.example.com` and `example.com` are separate entries. `*` matches no host.

Set `Annotate__TrustedStoryDomains__0` to the host. The next host is `Annotate__TrustedStoryDomains__1`.

Read [docs/design.md](../design.md) and [docs/vocabulary.md](../vocabulary.md) before changing the code. Read [docs/ui.md](../ui.md) before changing `Components/` or `wwwroot/css/`.

`build/check.sh` runs `dotnet format --verify-no-changes`, `ast-grep scan`, `ast-grep test`, `build/source-limits.sh`, `dotnet build`, and `dotnet test`. The line script fails when a `.cs` or `.razor` file under `src/` or `tests/`, or a `.css` file under `src/Annotate.Web`, is over 500 physical lines. It skips `bin`, `obj`, `Migrations`, and `*.g.cs`.

Once, point Git at the hooks in this repo:

```bash
git config core.hooksPath .githooks
```

The hook runs `build/check.sh`.

Issues and pull requests are in the [contribution guide](../contributing/README.md).
