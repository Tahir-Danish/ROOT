# Team Cero — Safe Recovery Copy Instructions

## IMPORTANT

This pack is intentionally **additive**.

Do not use "Paste and Replace" for the entire `docs` directory.

### Copy these files as new files

Copy:
```text
docs/TEAM_CERO_RECOVERED_PROJECT_CONTEXT.md
docs/CERO_CURRENT_DECISIONS_AND_LESSONS.md
docs/CERO_TEAM_WORKFLOW_AND_AGENT_METHOD.md
docs/CERO_REALTIME_PIPELINE_CONCEPT.md
docs/RECOVERY_COPY_INSTRUCTIONS.md
RESTORATION_STATUS.md
```

into the Team Cero project.

## Agent rules

The original project rule may have been overwritten. This pack intentionally does NOT instruct you to blindly replace it.

Before changing `.agents/rules`, inspect the current file.

If the old Team Cero rule is missing or clearly damaged, use the recovered rule included in:

`.agents/rules/team-cero-xr-core-RECOVERED.md`

but treat it as a recovery candidate and review it before installing.

## Existing documents

The following are known preserved sources and should be treated as source material:
- TEAM_CERO_PROJECT_BRIEF.md
- TEAM_CERO_KICKOFF.md
- Team_Cero_XR_Development_Handoff.md
- GEG XR Hackathon conversation notes

If these already exist locally, leave them untouched.

## Current-vs-historical wording

The original materials contain an earlier Scan-based formulation.

Current product decision:
**live camera → real-time perception → contextual AR**

Do not overwrite history merely to make wording uniform.

## Before implementation resumes

Perform a read-only project audit:
1. list `docs/`;
2. list `.agents/rules/`;
3. read project-context docs;
4. inspect the live Unity project;
5. report discrepancies;
6. stop.

Do not make implementation changes during the audit.
