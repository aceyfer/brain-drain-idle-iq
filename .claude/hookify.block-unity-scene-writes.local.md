---
name: block-unity-scene-writes
enabled: true
event: file
conditions:
  - field: file_path
    operator: regex_match
    pattern: \.unity$
action: block
---

**Rule fired: block-unity-scene-writes**

This is a direct Edit/Write to a `.unity` scene file. Scene changes in this project go through the Editor (idempotent MenuItem tools) or an explicit hand-edit-and-verify pass, never a blind tool write.

If you're deliberately doing scene work this session, lift this rule first: set `enabled: false` in `.claude/hookify.block-unity-scene-writes.local.md`, or delete the file.
