---
name: checkpoint
description: End a work block - update docs/STATE.md, commit, and push to origin. Use when the user says checkpoint, save progress, commit/push, or ends a pass.
---

1. Edit `docs/STATE.md`: update "What exists", "Not done yet", "Decisions", "Next" and the date. Keep it under ~60 lines.
2. `git status --short`; make sure no `Library/`, `Temp/`, `Assets/Screenshots/` or `.blend1` files are staged.
3. Commit with a short imperative message describing the pass (one commit per pass).
4. `git push origin main`. If it fails, report the error; never force-push.
5. Reply with one line: commit message + pushed or not.
