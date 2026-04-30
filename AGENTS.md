# AGENTS.md

This file provides guidance to agents when working with code in this repository.

## Project Overview

Volatility is a platform-agnostic interface for *Burnout Paradise* resource files (textures, renderables, environment data, GUI popups, shaders, etc.). It imports binary resources from any supported platform (TUB/PC, BPR, X360, PS3) into a standardized YAML representation and exports YAML back to any target platform's binary format. Single .NET 9.0 console project.

The submodule `tools/libbndl-extractor` is required for the game-path autotest workflow — it pins `Bo98/libbndl` as a nested submodule, so clones must use `git submodule update --init --recursive`.

## Build / Run

```bash
dotnet build Volatility/Volatility.csproj
dotnet run --project Volatility/Volatility.csproj                # interactive REPL
dotnet run --project Volatility/Volatility.csproj -- <command>   # one-shot
```

Release publish mirrors CI (`.github/workflows/dotnet.yml`):
```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true Volatility/Volatility.csproj
```
The csproj sets `PublishSingleFile`, `PublishTrimmed`, and copies `tools/dxc/**` to the output — the DXC tree must be present for shader operations after publish.

## Testing

There is no unit-test framework in this repo. Correctness is verified by the built-in `autotest` command, which has two modes:

1. **Synthetic / path mode** (`autotest --format=<TUB|BPR|X360|PS3> [--path=<file>]`): constructs or loads a texture header, writes it out, re-imports the result, and reflects over every public property/field to compare exported vs. re-imported values. Mismatches print in red.
2. **Game mode** (`autotest --game=<dir>` or `--games=a|b|c`): drives [GameAutotestOperation.cs](Volatility/Operations/Autotest/GameAutotestOperation.cs) — extracts real bundles via `tools/libbndl-extractor` (or YAP with `--bundletool=YAP`), runs import/export per supported `ResourceType`, and for the types in `RoundTripTypes` performs **exact binary parity** checks against the original bundle-extracted files. Useful flags: `--resourcelimit`, `--bundlelimit`, `--keepartifacts`, `--recap=<file|dir>` (writes a markdown recap).

When changing a resource's read/write path, the game-mode autotest on a real Burnout install is the authoritative parity check — synthetic mode only exercises textures. A green synthetic run is not evidence that non-texture types still round-trip.

## Architecture

### Command dispatch ([Frontend.cs](Volatility/Frontend.cs))
`Main` either enters a REPL or runs one tokenized command. The command registry is the static dictionary `Frontend.Commands` mapping lowercase name → `ICommand` type; commands are instantiated via `Activator.CreateInstance`. Args are parsed as `--key=value` or bare `--flag` (defaulted to `true`) into a `Dictionary<string, object>` passed to `ICommand.SetArgs`. Every command implements static `CommandToken`/`CommandDescription`/`CommandParameters` used by `HelpCommand` via reflection in [TypeUtilities](Volatility/Utilities/TypeUtilities.cs). **Adding a new command requires registering it in `Frontend.Commands` — there is no auto-discovery.**

### Resource model ([Resources/](Volatility/Resources/))
`Resource` (abstract base) → per-type abstract class (e.g. `TextureBase`, `RenderableBase`) → per-platform concrete class (e.g. `TexturePC`, `TextureBPR`, `TextureX360`, `TexturePS3`). Concrete classes override `ResourceEndian`, `ResourcePlatform`, and implement `ParseFromStream`/`WriteToStream`. Many also implement `PushAll`/`PullAll` to sync between a platform-specific struct and the portable fields inherited from the base.

Two attributes drive discovery and construction:
- `[ResourceDefinition(ResourceType.Foo)]` on the base class (or any class in the hierarchy) — maps the class to a `ResourceType` enum. Read via [ResourceMetadata.cs](Volatility/Resources/ResourceMetadata.cs).
- `[ResourceRegistration(RegistrationPlatforms.X, EndianMapped = true, PullAll = true)]` on concrete classes — specifies which platforms that class serves. `EndianMapped = true` makes [ResourceFactory](Volatility/Resources/ResourceFactory.cs) pick the `(string, Endian)` constructor and pass the platform's default endianness (BE for X360/PS3, LE for TUB/BPR). `PullAll = true` auto-invokes `PullAll()` after construction.

[ResourceFactory](Volatility/Resources/ResourceFactory.cs) builds a `(ResourceType, Platform) → Func<string, Resource>` map at startup by reading those attributes via reflection. **Every new resource class must be added to `AddRegisteredResource<>` calls in `CreateResourceCreators()`** — there is no assembly scan. The two registration sites (attribute + factory list) must stay in sync.

The `Arch` enum (x32/x64) distinguishes 32-bit pointer layouts (all original releases, most BPR) from 64-bit console BPR. Import commands accept suffixes like `bprx64`. Write pointer-width fields via `ResourceBinaryWriter.WritePointer(value, ResourceArch)` — the same applies to any struct whose size depends on `Arch`. Don't hardcode 4-byte pointer writes.

### Endianness ([Endian.cs](Volatility/Endian.cs), [Utilities/EndianAware*](Volatility/Utilities/))
Readers and writers are endian-aware. `Endian.Agnostic` means "follow the caller's intent"; concrete resources override `ResourceEndian` when the platform forces BE/LE. `EndianMapping.GetDefaultEndian(Platform)` is the canonical platform→endian lookup. Never swap bytes manually — use `EndianAwareBinaryReader`/`Writer` or `ResourceBinaryReader`/`Writer`.

### Operations layer ([Operations/](Volatility/Operations/))
CLI command classes are thin: they parse args and delegate to `Operations/` classes (e.g. `ImportResourceCommand` → `ImportResourceOperation`). Place real import/export/port logic in an `Operation` class, not in the CLI class.

### YAML serialization ([Utilities/YAML/](Volatility/Utilities/YAML/))
YamlDotNet is used with custom type inspectors/converters so that fields (not just properties) and `StrongID`/`ResourceID`/`BitArray` types round-trip correctly. The editor attributes in [Attributes/](Volatility/Attributes/) (`EditorCategory`, `EditorLabel`, `EditorTooltip`, `EditorHidden`, `EditorReadOnly`) are metadata-only today — they're consumed by the YAML layer and reserved for a future GUI. New public fields/properties that should appear in the YAML surface get `EditorCategory`/`Label`/`Tooltip`; derived or runtime-only members get `EditorHidden`/`EditorReadOnly` rather than being silently serialized.

### Runtime layout
At runtime, `EnvironmentUtilities.GetEnvironmentDirectory(...)` resolves paths relative to the executable:
- `tools/` — `dxc/` (shader compiler, copied from `Volatility/tools/dxc/`) and `libbndl-extractor/` (submodule; only needed for `autotest --game=...`)
- `data/ResourceDB/` — resource-ID → asset-name lookup used during import
- `data/Resources/` — default output destination for imported YAML
- `data/Splicer/` — Splicer resource data

`WorkspaceUtilities.FindRepositoryRoot()` walks up from CWD or the executable until it finds `Volatility.sln`; the autotest uses this to locate `tools/libbndl-extractor`. Don't hardcode path strings — resolve runtime paths through `GetEnvironmentDirectory`, and any repo-root lookup through `FindRepositoryRoot()`.

## Agent behavior

This project is actively being matured.

Do not treat the existing implementation as inherently correct, stable, or worth preserving.

Treat internal compatibility as a liability unless compatibility is explicitly declared as a requirement.

Prefer structural simplification over preserving old code paths.

Do not preserve:

- legacy code paths
- compatibility shims
- duplicated flows
- obsolete APIs
- old state models
- workaround behavior
- fallback behavior that hides the primary failure

Assume internal backward compatibility is not required.

Compatibility is required only for:

- public APIs explicitly marked stable
- persisted user data
- database migrations
- external integrations
- documented plugin interfaces
- binary format compatibility required for import/export correctness
- behavior the user explicitly says must remain supported

Everything else may be changed, renamed, removed, or consolidated when doing so improves the structure.

## Debugging and fixes

For bugs, regressions, broken behavior, failing tests, broken import/export paths, or unclear implementation requests:

1. Diagnose before editing.
2. Trace the relevant execution path before proposing a fix.
3. Identify the root cause, not just the nearest failing symptom.
4. Identify whether the current path is legacy, transitional, duplicated, or structurally wrong.
5. Present options before implementation when the change affects architecture, binary layout, state flow, resource parsing, serialization, command behavior, platform behavior, or shared utilities.
6. Do not write code until the chosen approach is clear.

Avoid:

- adding fallback logic without proving why the primary path fails
- adding duplicate state to mask synchronization problems
- weakening tests or parity checks to match broken behavior
- adding special cases before checking the general path
- broad refactors unrelated to the root cause
- adapters, bridges, fallbacks, feature-detection branches, or dual implementations unless there is a stated migration need
- preserving old code solely because existing callers still use it internally

Before coding, provide:

- root cause
- evidence
- affected files/functions
- relevant execution path
- legacy, duplicated, or obsolete paths involved
- what should become the new canonical path
- what should be removed
- what breakage is acceptable
- compatibility requirements, if any
- fix/refactor options
- recommended approach
- verification plan

After coding, provide:

- changed files
- reason for each change
- why this fixes the root cause
- legacy paths removed
- duplicate logic eliminated
- whether the new canonical path is used consistently
- tests/checks performed

## Codebase maturation policy

When solving an issue:

1. Identify whether the existing path is legacy, transitional, duplicated, or structurally wrong.
2. If a cleaner replacement exists, remove the old path instead of adapting around it.
3. Do not maintain backward compatibility for internal APIs, components, data shapes, commands, utilities, resource classes, or state flows unless the task explicitly says compatibility is required.
4. Do not add adapters, bridges, fallbacks, feature-detection branches, or dual implementations unless there is a stated migration need.
5. Prefer one canonical path.
6. Update all call sites to the canonical path.
7. Delete unused compatibility helpers.
8. Update tests, autotest expectations, docs, and command help to assert the new intended behavior, not legacy behavior.

## Investigation-only mode

When asked to investigate, diagnose, audit, plan, or review:

- Do not modify files.
- Do not write code.
- Trace the relevant execution path.
- Identify the actual source of the behavior.
- List the files/functions involved and why each matters.
- Present 2-4 possible fixes.
- For each fix, explain:
  - what it changes structurally
  - what risk it introduces
  - whether it is a local patch or architectural fix
  - whether it preserves or removes legacy behavior
  - how to verify it
- Recommend one option.
- If the root cause is uncertain, say so and list what evidence is missing.

## Implementation mode

When implementing an approved plan:

- Implement only the selected option.
- Keep the diff focused.
- Do not preserve broken structure just to reduce the diff.
- Remove obsolete paths instead of supporting both.
- Update all call sites to the canonical path.
- Do not add guards, retries, fallbacks, duplicate state, or special cases unless they address the root cause.
- Do not make tests pass by weakening the test or adapting around the failure.
- Update tests and documentation to match the new intended behavior.

After implementation, report:

1. changed files
2. why each change was necessary
3. how this addresses the root cause
4. what legacy behavior was removed
5. what tests or manual checks verify it

## Working style and hygiene

Surface tradeoffs and ask when requirements are ambiguous.

Do not silently pick one interpretation when the choice affects binary compatibility, resource format behavior, architecture, or public command behavior.

Ship the minimum code that solves the real problem.

Minimum code does not mean preserving bad structure. If a slightly larger change removes a bad path and creates a better canonical path, prefer the structural fix.

No unrequested configurability.

No error handling for impossible scenarios.

Do not restyle unrelated code.

Do not perform broad refactors unrelated to the request.

Do not delete unrelated dead code silently. If nearby obsolete code should be removed but is outside the current scope, mention it.

Match surrounding style.

Do not add comments unless the surrounding code already uses comments for the same kind of logic, or the logic is format-specific and non-obvious.

## Cross-cutting hygiene checks

After a change, check the following:

- Does a local helper duplicate existing boilerplate in `BitReader`, `ResourceBinaryReader`, `ResourceBinaryWriter`, `EndianUtilities`, `ResourceIDUtilities`, `TypeUtilities`, or the YAML helpers? If so, use the existing boilerplate.
- If the helper is a good generalization, should it be promoted to a shared utility instead of staying local?
- Do new struct read/write paths follow the same shape as `EnvironmentKeyframe`?
- If a struct read/write path deviates from the common shape, is the structure genuinely too specialized for that form?
- Did this change touch code that predates the current boilerplate and could now be simplified by adopting it?
- Are any introduced constants referenced from only one site?
- Do any introduced constants encode a value already named elsewhere?
- Did the change introduce a second path where one canonical path would be better?
- Did the change preserve legacy behavior without an explicit reason?
- Did the change hide a parsing, writing, endian, pointer-width, or serialization failure behind a fallback?

## Registration and layering reminders

These systems have no compile-time enforcement and are easy to miss:

- New commands must be wired into `Frontend.Commands`.
- New resource classes need both attributes and `ResourceFactory` registration.
- Pointer-width fields must use `WritePointer(..., ResourceArch)` or equivalent architecture-aware logic.
- Endian I/O must use endian-aware readers/writers.
- CLI classes should stay thin and delegate real work to `Operations/`.
- YAML-visible members need the appropriate editor attributes.
- Runtime paths should go through `EnvironmentUtilities`.
- Repository-root lookup should go through `WorkspaceUtilities.FindRepositoryRoot()`.

## Binary format discipline

Binary compatibility with the original game formats matters.

Do not confuse internal code compatibility with file-format compatibility.

Internal APIs may be changed aggressively when that improves the codebase.

Binary import/export behavior must remain correct for supported platforms unless the user explicitly asks to change support.

When modifying binary parsing or writing:

- account for platform
- account for endian
- account for pointer width
- account for alignment/padding
- account for versioned layouts
- account for resource-specific struct sizes
- verify with the strongest available autotest mode

Do not add a parser fallback merely because a file fails to parse.

First determine whether the failure is caused by:

- wrong platform detection
- wrong endian
- wrong architecture
- wrong struct size
- wrong offset
- wrong count
- wrong version assumption
- wrong resource type
- damaged or unsupported input

## Preferred fix shape

Prefer this sequence:

1. understand the format or code path
2. identify the root cause
3. choose the canonical model
4. remove or replace the wrong path
5. update all callers
6. verify round-trip behavior
7. delete obsolete helpers or compatibility branches

Avoid this sequence:

1. observe failure
2. add guard
3. add fallback
4. preserve old path
5. make output appear valid without proving correctness

## Agent behavior

This project is actively being matured.

Do not treat the existing implementation as inherently correct, stable, or worth preserving.

Treat internal compatibility as a liability unless compatibility is explicitly declared as a requirement.

Prefer structural simplification over preserving old code paths.

Do not preserve:

- legacy code paths
- compatibility shims
- duplicated flows
- obsolete APIs
- old state models
- workaround behavior
- fallback behavior that hides the primary failure

Assume internal backward compatibility is not required.

Compatibility is required only for:

- public APIs explicitly marked stable
- persisted user data
- database migrations
- external integrations
- documented plugin interfaces
- binary format compatibility required for import/export correctness
- behavior the user explicitly says must remain supported

Everything else may be changed, renamed, removed, or consolidated when doing so improves the structure.

## Debugging and fixes

For bugs, regressions, broken behavior, failing tests, broken import/export paths, or unclear implementation requests:

1. Diagnose before editing.
2. Trace the relevant execution path before proposing a fix.
3. Identify the root cause, not just the nearest failing symptom.
4. Identify whether the current path is legacy, transitional, duplicated, or structurally wrong.
5. Present options before implementation when the change affects architecture, binary layout, state flow, resource parsing, serialization, command behavior, platform behavior, or shared utilities.
6. Do not write code until the chosen approach is clear.

Avoid:

- adding fallback logic without proving why the primary path fails
- adding duplicate state to mask synchronization problems
- weakening tests or parity checks to match broken behavior
- adding special cases before checking the general path
- broad refactors unrelated to the root cause
- adapters, bridges, fallbacks, feature-detection branches, or dual implementations unless there is a stated migration need
- preserving old code solely because existing callers still use it internally

Before coding, provide:

- root cause
- evidence
- affected files/functions
- relevant execution path
- legacy, duplicated, or obsolete paths involved
- what should become the new canonical path
- what should be removed
- what breakage is acceptable
- compatibility requirements, if any
- fix/refactor options
- recommended approach
- verification plan

After coding, provide:

- changed files
- reason for each change
- why this fixes the root cause
- legacy paths removed
- duplicate logic eliminated
- whether the new canonical path is used consistently
- tests/checks performed

## Codebase maturation policy

When solving an issue:

1. Identify whether the existing path is legacy, transitional, duplicated, or structurally wrong.
2. If a cleaner replacement exists, remove the old path instead of adapting around it.
3. Do not maintain backward compatibility for internal APIs, components, data shapes, commands, utilities, resource classes, or state flows unless the task explicitly says compatibility is required.
4. Do not add adapters, bridges, fallbacks, feature-detection branches, or dual implementations unless there is a stated migration need.
5. Prefer one canonical path.
6. Update all call sites to the canonical path.
7. Delete unused compatibility helpers.
8. Update tests, autotest expectations, docs, and command help to assert the new intended behavior, not legacy behavior.

## Investigation-only mode

When asked to investigate, diagnose, audit, plan, or review:

- Do not modify files.
- Do not write code.
- Trace the relevant execution path.
- Identify the actual source of the behavior.
- List the files/functions involved and why each matters.
- Present 2-4 possible fixes.
- For each fix, explain:
  - what it changes structurally
  - what risk it introduces
  - whether it is a local patch or architectural fix
  - whether it preserves or removes legacy behavior
  - how to verify it
- Recommend one option.
- If the root cause is uncertain, say so and list what evidence is missing.

## Implementation mode

When implementing an approved plan:

- Implement only the selected option.
- Keep the diff focused.
- Do not preserve broken structure just to reduce the diff.
- Remove obsolete paths instead of supporting both.
- Update all call sites to the canonical path.
- Do not add guards, retries, fallbacks, duplicate state, or special cases unless they address the root cause.
- Do not make tests pass by weakening the test or adapting around the failure.
- Update tests and documentation to match the new intended behavior.

After implementation, report:

1. changed files
2. why each change was necessary
3. how this addresses the root cause
4. what legacy behavior was removed
5. what tests or manual checks verify it

## Working style and hygiene

Surface tradeoffs and ask when requirements are ambiguous.

Do not silently pick one interpretation when the choice affects binary compatibility, resource format behavior, architecture, or public command behavior.

Ship the minimum code that solves the real problem.

Minimum code does not mean preserving bad structure. If a slightly larger change removes a bad path and creates a better canonical path, prefer the structural fix.

No unrequested configurability.

No error handling for impossible scenarios.

Do not restyle unrelated code.

Do not perform broad refactors unrelated to the request.

Do not delete unrelated dead code silently. If nearby obsolete code should be removed but is outside the current scope, mention it.

Match surrounding style.

Do not add comments unless the surrounding code already uses comments for the same kind of logic, or the logic is format-specific and non-obvious.

## Cross-cutting hygiene checks

After a change, check the following:

- Does a local helper duplicate existing boilerplate in `BitReader`, `ResourceBinaryReader`, `ResourceBinaryWriter`, `EndianUtilities`, `ResourceIDUtilities`, `TypeUtilities`, or the YAML helpers? If so, use the existing boilerplate.
- If the helper is a good generalization, should it be promoted to a shared utility instead of staying local?
- Do new struct read/write paths follow the same shape as `EnvironmentKeyframe`?
- If a struct read/write path deviates from the common shape, is the structure genuinely too specialized for that form?
- Did this change touch code that predates the current boilerplate and could now be simplified by adopting it?
- Are any introduced constants referenced from only one site?
- Do any introduced constants encode a value already named elsewhere?
- Did the change introduce a second path where one canonical path would be better?
- Did the change preserve legacy behavior without an explicit reason?
- Did the change hide a parsing, writing, endian, pointer-width, or serialization failure behind a fallback?

## Registration and layering reminders

These systems have no compile-time enforcement and are easy to miss:

- New commands must be wired into `Frontend.Commands`.
- New resource classes need both attributes and `ResourceFactory` registration.
- Pointer-width fields must use `WritePointer(..., ResourceArch)` or equivalent architecture-aware logic.
- Endian I/O must use endian-aware readers/writers.
- CLI classes should stay thin and delegate real work to `Operations/`.
- YAML-visible members need the appropriate editor attributes.
- Runtime paths should go through `EnvironmentUtilities`.
- Repository-root lookup should go through `WorkspaceUtilities.FindRepositoryRoot()`.

## Binary format discipline

Binary compatibility with the original game formats matters.

Do not confuse internal code compatibility with file-format compatibility.

Internal APIs may be changed aggressively when that improves the codebase.

Binary import/export behavior must remain correct for supported platforms unless the user explicitly asks to change support.

When modifying binary parsing or writing:

- account for platform
- account for endian
- account for pointer width
- account for alignment/padding
- account for versioned layouts
- account for resource-specific struct sizes
- verify with the strongest available autotest mode

Do not add a parser fallback merely because a file fails to parse.

First determine whether the failure is caused by:

- wrong platform detection
- wrong endian
- wrong architecture
- wrong struct size
- wrong offset
- wrong count
- wrong version assumption
- wrong resource type
- damaged or unsupported input

## Preferred fix shape

Prefer this sequence:

1. understand the format or code path
2. identify the root cause
3. choose the canonical model
4. remove or replace the wrong path
5. update all callers
6. verify round-trip behavior
7. delete obsolete helpers or compatibility branches

Avoid this sequence:

1. observe failure
2. add guard
3. add fallback
4. preserve old path
5. make output appear valid without proving correctness
