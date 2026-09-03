# Team Cero — AI/ML ↔ XR Contract

## Responsibility boundary

P/ML owns:
- dataset;
- training/evaluation;
- inference;
- confidence estimation;
- failure/unknown handling.

XR owns:
- camera/AR runtime;
- receiving structured results;
- mapping species IDs to presentation data;
- contextual UI/AR behavior;
- Botanical Guide handoff.

## Canonical result contract

### Success
```json
{
  "species_id": "SP01",
  "confidence": 0.91,
  "status": "identified"
}
```

### Low confidence / uncertain
```json
{
  "species_id": null,
  "confidence": 0.42,
  "status": "uncertain"
}
```

### Unsupported / failure
```json
{
  "species_id": null,
  "confidence": 0.0,
  "status": "unrecognized"
}
```

## Rules

- IDs are stable identifiers, not display names.
- Confidence is numeric and interpreted by the consuming layer according to an agreed threshold.
- The XR layer must have a safe state for unknown/low-confidence results.
- Do not hardcode training logic into Unity.
- Do not let UI text become the contract between ML and XR.
