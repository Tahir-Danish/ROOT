# Team Cero — Agent Task Protocol

## Before acting

For any non-trivial task, determine:
1. desired product outcome;
2. owning layer;
3. relevant project docs;
4. actual live Unity state;
5. known vs inferred;
6. smallest safe change/experiment.

## Layer classification

```text
L0 Product intent
L1 Project architecture
L2 Source asset / import
L3 Unity asset / hierarchy
L4 Prefab / component configuration
L5 Template integration
L6 Runtime state / interaction
L7 Camera / perception input
L8 ML inference
L9 API / software integration
L10 Data / knowledge
L11 Device performance / deployment
```

Find the lowest layer that explains the problem before changing a higher layer.

## Investigation mode

- read docs
- inspect live state
- inspect source code when needed
- gather evidence
- do not modify
- do not create files unless explicitly requested
- stop at diagnosis

## Implementation mode

1. inspect
2. state intended change
3. identify exact target
4. make smallest change
5. verify serialized/editor state
6. compile/import check
7. runtime test when relevant
8. report exact changes

## Hypothesis mode

Replace:
> X is the cause.

with:
> X is the leading hypothesis because...

Then identify the smallest experiment that distinguishes it.

## Existing-system preference

Preserve working Mobile AR Template architecture.

Do not replace existing systems because a new system appears easier.

Before adding a package, script, manager or service, determine whether the current stack already provides the needed capability.

## Real-time perception rule

Do not introduce a manual Scan/capture workflow merely because it is easier.

Target:
```text
live camera → perception → detection state → AR augmentation
```

Mocks are allowed for learning but must be clearly labeled as mocks.

## Data contract rule

Keep:
- perception
- verified knowledge
- transport/service
- Unity presentation

separable.

## Verification rule

Distinguish:
- editor verified
- compile/build verified
- physical-device verified

Do not claim device behavior from editor inspection.

## Documentation rule

When an experiment changes architectural understanding:
- propose/update the documentation as appropriate;
- do not silently turn temporary observations into permanent requirements.

## Communication style

Report:
- what was inspected
- what is known
- what is inferred
- what changed
- how verified
- what remains unknown
