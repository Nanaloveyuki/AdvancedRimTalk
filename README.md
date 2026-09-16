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

Takeover is a fully custom message path. It does not promise RimTalk's original decoration semantics. Each enabled Prompt Part executes `{{% ... %}}` Arti first, followed by RimTalk's native `{{ ... }}` Scriban renderer. RimTalk still owns talk triggering, participant selection, AI client calls, and response handling. Its active preset assembly is bypassed, but Scriban is not replaced.

Prompt Parts are the only persisted takeover content source. Their order, enabled state, roles, and content control the messages. Legacy document-only Advanced RimTalk settings are not migrated. The preset importer preserves template source, including native Scriban, without automatically translating expressions. It does not currently preserve all positional and main-history metadata, so importing a preset is not a guarantee of equivalent behavior.

Defaults include base instructions, JSONL output instructions, context, dialogue state, history, and the raw dialogue request. These are editable parts, not mandatory RimTalk decorations. Language instructions, mood/social effects, and the original history message sequence are not automatically preserved. Templates remain responsible for producing output compatible with RimTalk's response parser.

Takeover settings bound pawn context count (default 32), history count (40), and final prompt characters (24,000). Character budgeting shares space between parts, preserves short parts where possible, and logs truncation. It is not a tokenizer or a guarantee that a model's context window will fit. Context construction uses a thread-local settings copy with a separate Context object, rather than replacing the shared settings field. This is a synchronous context-building scope, not an asynchronous settings override. Host-double checks cover nesting, exception cleanup, duplicate disposal, and thread isolation; they do not verify Harmony installation in the game.

## Optional memory integration

ExpandMemory write helpers are experimental, compatibility-sensitive APIs. They change the provider's memory or common-knowledge data. Callers must check returned results; success is not guaranteed across versions or memory types, and multi-field updates are not transactions. Exceptions routed through the bridge's operation logger are visible outside DevMode. Pinning uses the provider's maintainer; moving out of Active privatizes entries before layer migration. These paths still require focused integration tests and game/save validation.

## Settings UI

When `Nanaloveyuki.IrisMenus` is active, Advanced RimTalk registers its settings page plus separate `Arti Editor`, `Arti REPL`, and `Arti Documentation` SubItems through IrisMenus' public API. The Editor edits and analyzes the takeover document without executing it; the REPL keeps its own history and executes Arti against the current selection/map without creating a RimTalk talk request. The Documentation page embeds the Arti Markdown library, builds its navigation from the maintained documentation indexes, and presents categories and pages in a two-column layout. Enter runs in the REPL and Shift+Enter inserts a newline. The integration is optional and reflection-based: no `IrisMenus.dll` is shipped with this mod, and the original RimWorld settings dialog remains the fallback when IrisMenus is absent or incompatible.

`Prompt Parts` opens a focused Arti Editor window for the selected part. Edits write directly to that part; the standalone Editor targets the first System part. Opening the editor does not execute a prompt.

Takeover presets are stored separately from RimTalk presets. Existing takeover parts become the first preset without losing their content. The preset list supports creating, duplicating, renaming, deleting, and activating presets. Selecting a preset edits it; only Activate changes the runtime preset. Local/shared imports create a separate takeover preset instead of appending to the current parts. The redundant System document field has been removed from the main settings page.

Prompt Preview identifies the active mode/preset separately from the preview selection. Context previews can render another preset without activating it. Captured messages and generated context previews are separate results, labeled with their source preset and capture/generation time. Output uses a dark, selectable plain-text surface; rich-text-looking prompt content remains literal.

The documentation reader lays out Markdown tables and opens local Markdown links, with a Back action. HTTP(S) links open in the browser. IrisMenus search indexes document titles and paths and opens the selected document directly. Source mode and Copy all retain the Markdown text for copying.

`Prompt Preview` offers raw message-array JSON and plain text, with separate embed/takeover selection. This JSON describes prompt messages, not an AI response or a complete provider request body. By default it shows the latest messages generated in the current game. Viewing a capture does not execute scripts, write memories, or call the model. Outside a running game, or before a matching result exists, the page shows an empty state.

Experimental context execution is off by default. When enabled in a running game, it renders current templates against the selected pawn, or a colonist on the current map, and the entered preview request. Available pawn context and history are included; failed context reads and template errors appear separately from the output. Erroring Arti blocks and unresolved Scriban entries are skipped. Embed uses RimTalk preset assembly and request decoration; takeover uses its configured parts and message budget. Results are labeled as context previews, not captured requests. They are not an exact replay of every event, announcement, participant group, or third-party hook.

Active previews use isolated Arti/Scriban session variables and reject the bridge's explicit memory mutation calls. This is not a sandbox for arbitrary native Scriban or third-party callbacks, which is why context execution remains experimental. No model request is sent by the preview action.

## Arti language design

The formal language and Prompt document design is maintained in [`docs/Arti/zh_cn/index.md`](docs/Arti/zh_cn/index.md). `tmp/dsl.md` remains an experimental sketch. The RimWorld runtime provider exposes game data under `core.game`, `core.world`, `core.maps`, `core.pawns`, `core.factions`, `core.settlements`, `core.world_objects`, `core.mods`, `core.defs`, `core.query`, and `core.find`. When RimTalk - Expand Memory is installed and active, Arti can import the optional `memory` module for its reflected memory and common-knowledge API.

The actual Arti document parser extracts only `{{% ... %}}` blocks. It preserves source locations, ignores Markdown fenced/inline code and native Scriban blocks, and leaves all text outside Arti blocks unchanged. The RimTalk symbol adapter reads the installed RimTalk variable registry at runtime; RimTalk - Expand Memory is discovered through reflection when present, so no compile-time reference is shipped.

Arti functions and `const` declarations are registered in a per-game global instance and can be referenced by later Arti blocks. The registry is recreated when the loaded game changes and is not serialized. Repeating the same declaration is idempotent; a different owner or source cannot replace an existing name. Ordinary `let` variables are block-local. Global functions resolve runtime data when called, so use a function such as `fn numbers() { return a }` for dynamic values instead of exposing mutable public state. `const` initializers must be static expressions; dynamic game values belong in functions.

For detailed Pawn data, use the read-only `pawn.info` object (or `core.pawn.info` for the current Pawn). It exposes structured age decomposition, health state, all Hediffs including hidden ones, needs, prompt context, and the public Pawn trackers; existing `pawn.age`, `pawn.health`, and raw tracker paths remain compatible.

## Build

The default build properties point at the local RimWorld 1.6, RimTalk, and Harmony assemblies. Override `RimWorldManagedDir`, `RimTalkAssemblyDir`, and `HarmonyAssemblyPath` when building on another machine.

```powershell
dotnet build AdvancedRimTalk.csproj -c Release
dotnet run --project Tests/PromptChecks.csproj -c Release
```
