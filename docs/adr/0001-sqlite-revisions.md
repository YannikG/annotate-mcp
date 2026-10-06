# SQLite revisions

Status: accepted

## Context

Annotate needs durable, queryable revisions, blocks, and reviews. JSON files were set aside because browsing by project and keeping every revision is awkward.

## Decision

Store the data in one SQLite file. Revisions are immutable. Block keys stay stable so a later edit can target a block. Plans and Reviews each own a DbContext. There are no cross-feature foreign keys. Reviews stores `revision_id` as a plain value.

## Consequences

Migrations run at startup against that one file. A review can point at a revision without a foreign key into the Plans schema. Block identity survives from one revision to the next, which is what a later in-place edit needs.
