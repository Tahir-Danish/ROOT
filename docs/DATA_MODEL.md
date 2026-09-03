# Team Cero — Data Model

## Species record

A species entry should support at least:

- species_id
- common_name
- scientific_name
- family
- native_region
- ecological_importance
- conservation_status
- threats
- conservation_best_practices
- source_references
- approved_for_mvp

## Example shape

```json
{
  "species_id": "SP01",
  "common_name": "TBD",
  "scientific_name": "TBD",
  "family": "TBD",
  "native_region": "TBD",
  "ecological_importance": "TBD",
  "conservation_status": "TBD",
  "threats": [],
  "conservation_best_practices": [],
  "source_references": [],
  "approved_for_mvp": false
}
```

## Source discipline

Plant facts must be source-verified by M before becoming presentation data. Do not invent native status, conservation status, or ecological claims to fill blanks.

## Dataset layers

1. `data/species/species_registry.csv` — presentation/knowledge records
2. `data/ml/class_manifest.json` — classifier classes and stable IDs
3. `data/ml/image_manifest.csv` — training/evaluation image metadata
4. `data/xr/test_models.csv` — playground assets only
