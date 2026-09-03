# Team Cero — Antigravity Prompt Guide

## Prompt structure

Use:

**Goal → Context → Constraints → Process → Success criterion → Scope of verification**

## Good example

```text
Goal:
Make one custom prefab use the same manipulation behavior as the template's working object.

Constraints:
- Do not replace the existing AR placement system.
- Do not add packages.
- Do not modify AR Foundation configuration.
- Reuse the template's existing interaction architecture.

Process:
1. Inspect the working template object.
2. Inspect the custom prefab.
3. Identify the exact component/configuration difference.
4. Propose the smallest change.
5. Modify only after approval.
6. Compile and verify.

Success:
The custom prefab can be placed, moved, rotated, and scaled like the working template object, without breaking existing objects.
```

## Bad examples

- "Fix the AR system."
- "Make this better."
- "Build the whole plant app."
- "Refactor everything for scalability."

These are under-specified and encourage speculative changes.

## Read-only test prompt

```text
Inspect the relevant implementation and explain how it works. Do not modify anything, create files, install packages, or reconfigure the project. Cite the exact GameObjects/components/scripts/assets you inspected and separate observed facts from inference.
```

## Change discipline

When a task touches a working system, require the agent to identify the current known-good behavior before changing it.
