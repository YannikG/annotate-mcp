# Contribution guide

An issue comes first. Say what is wrong or missing, and why it is worth changing. Blank issues are off.

When the change alters product behaviour, cite the matching public spec in the issue, for example `Spec: docs/specs/review/review-a-revision.md`. Update that spec (and the linked journey steps if the path changed) in the same pull request. New behaviour needs a short journey plus spec under `docs/journeys/` and `docs/specs/`: one-sentence story and steps in the journey; must / must not, flow, and verification in the spec.

Check the wiki pages under `docs/wiki/` and the other guides under `docs/` in the same pull request when the change affects what people read there. `build/publish-wiki.sh` is the map from those files to wiki page names; update it when you add or rename a published page.

Pull requests are limited to repository administrators, maintainers, and the GitHub logins in `CONTRIBUTORS.md`. That file is one login per line. A line added in the same pull request does not count.

`main` moves only by squash-merging a pull request. Merge commits and rebase merges are off. The ruleset applies to administrators. A direct push to `main` is refused.

The pull request body links an issue in this repository, for example `https://github.com/YannikG/annotate-mcp/issues/1`.
