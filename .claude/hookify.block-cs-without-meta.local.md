---
name: block-cs-without-meta
enabled: true
event: bash
conditions:
  - field: command
    operator: regex_match
    pattern: git\s+add\s+.*\.cs(["']|\s|$)
  - field: command
    operator: not_contains
    pattern: .meta
action: block
---

**Rule fired: block-cs-without-meta**

This command stages a `.cs` file but no `.meta` file appears anywhere in the same command. New/changed `.cs` files must always ship with their `.meta` in the same commit.

Add the matching `.cs.meta` path to this same `git add` command.

(Note: this rule checks whether *any* `.meta` path is present in the command, not that it's the exact matching one for each `.cs` file -- double-check the pairing yourself on multi-file adds.)
