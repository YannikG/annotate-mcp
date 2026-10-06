# Installation guide

Someone who only wants to use the MCP server needs Docker and the published image. The .NET SDK, a clone, and Compose are not required.

The published image does not exist until the first `v*` tag. Until then, use [manual deployment](../deployment/README.md). The agent pages still apply once something is listening on `127.0.0.1:24173`.

## Run

```bash
docker volume create annotate-data
docker run --rm --read-only \
  --name annotate \
  -p 127.0.0.1:24173:24173 \
  -v annotate-data:/data \
  --tmpfs /tmp \
  -e ASPNETCORE_URLS=http://0.0.0.0:24173 \
  -e Annotate__DataDir=/data \
  -e Annotate__OpenBrowser=false \
  -e HOME=/tmp \
  ghcr.io/yannikg/annotate-mcp:latest
```

The site is at http://127.0.0.1:24173. MCP is at http://127.0.0.1:24173/mcp.

Use a `v*` tag instead of `latest` when you want a fixed release.

## Data

`annotate.db` lives on the `/data` volume, named `annotate-data` in the command above.

## Story hosts

With no story-host setting, every story URL is refused. A host matches that hostname only, so `docs.example.com` and `example.com` are separate entries. `*` matches no host.

Add one `-e` per host:

```bash
  -e Annotate__TrustedStoryDomains__0=jira.example.net \
  -e Annotate__TrustedStoryDomains__1=github.com \
```

## Agents

Connect the server, then paste the rule that makes that agent call `annotate_plan`.

| Agent | Page |
| --- | --- |
| Claude Code | [claude-code/README.md](claude-code/README.md) |
| Codex | [codex/README.md](codex/README.md) |
| Cursor | [cursor/README.md](cursor/README.md) |
| OpenCode | [opencode/README.md](opencode/README.md) |
