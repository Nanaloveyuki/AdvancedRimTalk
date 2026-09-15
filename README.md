# Advanced RimTalk - Prompt

This is the prompt-focused first slice of Advanced RimTalk. It does not replace RimTalk's trigger system, request pipeline, or model client. The settings page provides two prompt integration modes: embed Arti into the existing RimTalk prompt, or let Advanced RimTalk take over prompt message construction.

## Current behavior

In embed mode RimTalk still owns prompt entry order, `system`/`user`/`assistant` roles, chat history placement, and third-party prompt entries. When RimTalk renders an entry, Advanced RimTalk executes formal `{{% ... %}}` Arti blocks first, then preserves the existing Scriban pipeline. The older `art.*` expressions remain available as a compatibility layer.

The expansion runs against a snapshot captured for that render. Inserted text is protected until Scriban finishes, so a value containing `{{ ... }}` is not evaluated a second time.

Supported compatibility placeholders:

```text
{{ art.pawn_name }}
{{ art.recipient_name }}
{{ art.talk_type }}
{{ art.dialogue_type }}
{{ art.intent }}
{{ art.topic }}
{{ art.status }}
{{ art.prompt }}
{{ art.raw_prompt }}
{{ art.context }}
{{ art.pawn_context }}
{{ art.tick }}
{{ art.hour }}
{{ art.date }}
{{ art.season }}
{{ art.weather }}
{{ art.temperature }}
{{ art.wealth }}
{{ art.is_announcement }}
{{ art.is_monologue }}
{{ art.state }}
```

Common functions support both parenthesized and whitespace argument forms:

```text
{{ art.random_int(1, 6) }}
{{ art.random_float(0, 1) }}
{{ art.choose("calm", "urgent", "curious") }}
{{ art.default(art.topic, "unknown topic") }}
{{ art.join(", ", "name", art.pawn_name) }}
```

Multiline calls are supported:

```text
{{ art.random_int(
  1,
  6
) }}
```

This syntax is intentionally separate from native Scriban and from RimTalk's trigger system. The Arti executor uses an allowlisted runtime surface: prompt context values, RimTalk custom variables/hooks, module status, read-only RimWorld game data, output, text/escape helpers, and prompt-local random values. Game objects are exposed through public fields/properties and explicit query selectors; arbitrary methods are not invoked.

## Prompt builder replacement

The mod settings include a prompt integration mode. When takeover is selected, Advanced RimTalk intercepts `PromptManager.BuildMessages`, prepares the normal RimTalk request context, executes the configurable takeover Arti document, and builds the system and user messages itself. This bypasses RimTalk active presets, preset assembly, and Scriban rendering for that request; RimTalk still owns talk triggering, participant selection, AI client calls, and response handling.

The default takeover document emits a system instruction, the current pawn/dialogue context, and RimTalk's JSON output contract. The user message retains the current conversation history and decorated dialogue request. The document can be edited in the takeover settings; only `{{% ... %}}` blocks execute there, while other text is sent unchanged as the system message.

## Settings UI

When `Nanaloveyuki.IrisMenus` is active, Advanced RimTalk registers its settings page plus separate `Arti Editor`, `Arti REPL`, and `Arti Documentation` SubItems through IrisMenus' public API. The Editor edits and analyzes the takeover document without executing it; the REPL keeps its own history and executes Arti against the current selection/map without creating a RimTalk talk request. The Documentation page embeds the Arti Markdown library, builds its navigation from the maintained documentation indexes, and presents categories and pages in a two-column layout. Enter runs in the REPL and Shift+Enter inserts a newline. The integration is optional and reflection-based: no `IrisMenus.dll` is shipped with this mod, and the original RimWorld settings dialog remains the fallback when IrisMenus is absent or incompatible.

## Arti language design

The formal language and Prompt document design is maintained in [`docs/Arti/zh_cn/index.md`](docs/Arti/zh_cn/index.md). `tmp/dsl.md` remains an experimental sketch. The RimWorld runtime provider exposes game data under `core.game`, `core.world`, `core.maps`, `core.pawns`, `core.factions`, `core.settlements`, `core.world_objects`, `core.mods`, `core.defs`, `core.query`, and `core.find`. When RimTalk - Expand Memory is installed and active, Arti can import the optional `memory` module for its reflected memory and common-knowledge API.

The actual Arti document parser extracts only `{{% ... %}}` blocks. It preserves source locations, ignores Markdown fenced/inline code and native Scriban blocks, and leaves all text outside Arti blocks unchanged. The RimTalk symbol adapter reads the installed RimTalk variable registry at runtime; RimTalk - Expand Memory is discovered through reflection when present, so no compile-time reference is shipped.

For detailed Pawn data, use the read-only `pawn.info` object (or `core.pawn.info` for the current Pawn). It exposes structured age decomposition, health state, all Hediffs including hidden ones, needs, prompt context, and the public Pawn trackers; existing `pawn.age`, `pawn.health`, and raw tracker paths remain compatible.

## Build

The default build properties point at the local RimWorld 1.6, RimTalk, and Harmony assemblies. Override `RimWorldManagedDir`, `RimTalkAssemblyDir`, and `HarmonyAssemblyPath` when building on another machine.

```powershell
dotnet build AdvancedRimTalk.csproj -c Release
dotnet run --project Tests/PromptChecks.csproj -c Release
```
