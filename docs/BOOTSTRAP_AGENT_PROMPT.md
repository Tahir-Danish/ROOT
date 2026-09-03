# Team Cero — Bootstrap Agent Prompt

Use this once after the documentation kit is installed. It is intentionally read-only.

```text
You are joining the existing Team Cero Unity XR project.

Before making any changes, read the relevant project docs under /docs and inspect the live Unity project using Unity MCP.

Treat /docs as the source of truth for intent, requirements, architecture, decisions, and roadmap. Treat the live Unity project as the source of truth for implementation state.

Perform a read-only onboarding pass only.

Inspect:
- Unity version
- packages relevant to AR/XR/UI/input
- active scene
- relevant scene hierarchy
- AR Session
- XR Origin
- AR Camera
- AR Plane Manager
- AR Raycast Manager
- existing UI
- existing scripts
- relevant prefabs
- object spawning
- object selection
- object manipulation
- animation systems

Trace the current known-good flow from camera/tracking through plane detection, interaction, spawning, and manipulation.

Separate:
1. Unity/package/template functionality
2. Team Cero custom functionality
3. temporary experiments

Do not create files.
Do not edit files.
Do not rename/move/delete assets.
Do not install packages.
Do not reconfigure the project.
Do not fix anything.

When reporting findings, distinguish observed facts from inference and mark uncertainty explicitly.

Return:
1. Project intent
2. Current architecture
3. Current AR pipeline
4. Existing reusable systems
5. Known-good functionality
6. Experimental changes
7. Risks/inconsistencies
8. Recommended next learning experiment

Do not make any modifications until the user approves a specific next task.
```
