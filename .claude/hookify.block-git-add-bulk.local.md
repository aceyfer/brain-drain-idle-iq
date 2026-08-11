---
name: block-git-add-bulk
enabled: true
event: bash
pattern: add\s+-A(\s|$)|add\s+\.(\s|$)
action: block
---

**Rule fired: block-git-add-bulk**

This command stages files with `add -A` or `add .`. This project stages **explicit paths only** -- never bulk-add.

Re-run with each file path spelled out explicitly instead.
