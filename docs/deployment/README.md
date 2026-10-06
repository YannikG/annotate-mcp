# Manual deployment

Compose builds the image from the Dockerfile in this repo and publishes the site on http://127.0.0.1:24173. The process inside the container listens on `0.0.0.0:24173`. SQLite lives on the `annotate-data` volume, mounted at `/data`.

```bash
docker compose up
docker compose down
```

The container ships with no trusted story hosts. Add them in a gitignored `compose.override.yaml` beside `compose.yaml`. `docker compose up` merges it:

```yaml
services:
  annotate:
    environment:
      Annotate__TrustedStoryDomains__0: jira.example.net
```

`Annotate__TrustedStoryDomains__1` is the next host. A host matches that hostname only. `*` matches no host.
