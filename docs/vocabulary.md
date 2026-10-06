# Vocabulary

## Design words

**Module.** Anything with an interface and an implementation.

**Interface.** Everything a caller must know: types, invariants, order, errors, configuration, and cost.

**Implementation.** The body behind an interface.

**Seam.** Where the interface lives so behaviour can change without editing the caller.

**Adapter.** A concrete type satisfying an interface at a seam.

**Depth.** Behaviour per unit of interface.

**Leverage.** What callers get from depth.

**Locality.** What maintainers get from depth: one place to change.

## Product words

**Dashboard.** The start page. It lists pending plans from active projects, summarises review outcomes, and lists every project.

**Attribution.** The agent, model, and MCP client recorded on one revision.

**Project.** A group of plans keyed by a normalised folder path. The display name can be renamed. The path cannot.

**Plan.** A titled sequence of revisions in one project. It can be archived, which removes it from the active lists until it is restored, or deleted, which removes it and its reviews.

**Revision.** An immutable Markdown snapshot numbered 1..n.

**Block.** A top-level Markdown block stored with a revision.

**Block key.** A stable id reused across revisions when the block is unchanged or edited in place.

**Review.** A decision record for exactly one revision.

**Annotation.** A deletion, replacement, insertion, or comment anchored to plan offsets.

**Annotation draft.** An annotation saved against a pending review. It survives navigation but has not been submitted as feedback.

**Decision fence.** A `decision` code block asking one question.

**Feedback.** Text returned to the agent after changes are requested.

**Story link.** An optional absolute URL above the plan.

**Acceptance criteria.** Optional text above the plan. It is not annotatable.

**Composition root.** Annotate.Web, the only project that wires features.

**Shared project.** Listed in `build/shared-projects.txt`. Features may reference it. Features never reference each other.
