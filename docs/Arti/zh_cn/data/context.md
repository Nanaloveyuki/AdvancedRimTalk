# context

`ctx` 是当前 RimTalk 请求的上下文对象。它是编写提示词时最常用的数据入口：

```arti
core.emit(ctx.topic)
core.emit(ctx.dialogue_type)
```

没有对应信息时，成员可能返回 `null` 或空文本。输出前可以使用 [`default`](../core/default.md)。

## 常用成员

| 成员 | 含义 |
| --- | --- |
| `pawn` / `current_pawn` | 当前发起对话的 Pawn |
| `recipient` | 对话接收者，可能为空 |
| `pawns` / `all_pawns` | 当前上下文中的 Pawn 集合 |
| `map` | 当前地图，可能为空 |
| `talk_type` / `talktype` | 对话类型 |
| `dialogue_type` / `dialoguetype` | 对话子类型 |
| `intent` | 对话意图 |
| `topic` / `conversation_topic` | 对话话题 |
| `status` / `dialogue_status` | 对话状态 |
| `prompt` / `dialogue_prompt` | 当前对话提示 |
| `user_prompt` | 用户请求文本 |
| `context` / `pawn_context` | 当前 Pawn 的上下文文本 |
| `chat` / `history` | 聊天历史对象 |
| `is_monologue` | 是否为独白 |
| `pawn_count` | 上下文中的 Pawn 数量 |
| `map_id` | 当前地图 ID |

例如：

```arti
let topic = core.value.default(ctx.topic, "未指定")
let kind = core.value.default(ctx.dialogue_type, "普通对话")

core.emit("话题：" + topic, newline: true)
core.emit("类型：" + kind, newline: true)
```

## 直接可用的根名称

为了兼容 RimTalk，以下值也可以直接使用：

```arti
pawn
recipient
pawns
map
prompt
context
raw_prompt
chat
settings
game
```

此外还提供 `time`、`hour`、`day`、`quadrum`、`year`、`season`、`weather`、`temperature`、`wealth` 和 `events` 等上下文值。需要区分字段含义时，优先使用 `ctx`、`map` 或 [game](game.md) 下的明确成员。

`prompt` 是当前对话提示，`user_prompt` 是用户请求，`raw_prompt` 是装饰前的原始请求；`context` 和 `pawn_context` 是当前 Pawn 的上下文文本。

`json.format` 提供 RimTalk 当前使用的 JSON 回复约束文本，`lang` 是当前语言文本。它们都是生成时读取的只读值。

如果需要读取 RimTalk 设置对象，可以检查 `settings` 是否存在，再读取 `apply_mood_and_social_effects`、`use_advanced_prompt_mode`、`player_name`、`player_persona`、`simple_mode_instruction`、`player_dialogue_mode` 和 `context`。

`settings.player_name`（也支持 `settings.playername`）读取 RimTalk 的玩家称呼；字段为 `null` 时返回空字符串：

```arti
let player_name = settings.player_name
core.emit("玩家称呼：" + player_name, newline: true)
```

## 自定义上下文变量

RimTalk 或其他 Mod 可以通过上下文变量注册接口提供额外名称。可用名称取决于当前启用的 Mod；需要跨环境运行时，用 [`use optional`](../language/optional.md) 管理这类集成。
