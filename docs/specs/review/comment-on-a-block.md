---
status: accepted
sources:
  - https://github.com/YannikG/annotate-mcp/issues/9
---

# Spec: Comment on a block

## Must

- Let `annotate_block` add one comment on a whole block of a pending review, using the review id and a stored block key.
- Show that agent note on the block heading with a bot icon, starting not accepted.
- Let a person comment on a selected phrase.
- Let a person add any number of replies directly under an annotation.
- Let a person delete an annotation or a reply while the review is pending, after the shared confirm dialog. Deleting an annotation deletes its replies.
- Let a person toggle an agent note between accept and not accept while the review is pending.
- Keep Approve unavailable while any annotation remains.
- Drop unaccepted agent notes and their replies on Request changes, and ask with the confirm dialog first when any would be dropped.
- Include a kept block note as the block id, the comment, the replies, and one line `from: agent`.
- Keep a phrase edit in its current feedback form, plus `from: operator` and any replies.

## Must not

- Let a person comment on a whole block.
- Let the agent reply, open a decision, or annotate a phrase.
- Let a reply have replies.
- Let a person accept a note that is not from an agent.
- Merge two notes on the same block.
- Include an unaccepted agent note in the feedback.
- Change a review after Request changes or Approve.

## Flow

Agent calls `annotate_block` → person replies, accepts, or deletes → Request changes drops unaccepted agent notes → person clears every annotation → Approve.

## Verification

On a pending review, call `annotate_block` with a real block id and confirm the note is not accepted. Confirm the heading has no Comment button. Reply twice, delete one reply, toggle accept, and confirm Approve stays disabled. Request changes with an unaccepted agent note and confirm the dialog, then confirm the feedback omits that note and names `from: agent` on a kept block note.
