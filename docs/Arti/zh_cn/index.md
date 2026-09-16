# Arti

Arti 是 Advanced RimTalk 提供的提示词脚本语言。它让你可以在 RimTalk 的提示词中读取上下文、做判断和循环、整理游戏数据，然后把结果明确地写回提示词。

Arti 不替换 RimTalk 的触发器、参与者选择、模型客户端或回复处理。你仍然使用 RimTalk 管理对话，只把需要脚本化的提示词部分交给 Arti。

## 五分钟上手

在 RimTalk 的提示词条目中写入一个正式 Arti 代码块：

```text
今天的对话对象是：
{{%
core.emit(pawn.name, newline: true)
%}}
请保持简洁。
```

正式代码块使用 `{{%` 开始、`%}}` 结束。只有代码块中的内容会执行，其他文字会原样保留。

表达式只负责计算，输出由 [`emit`](core/emit.md) 完成：

```text
{{%
let topic = core.value.default(ctx.topic, "未指定")
core.emit("话题：" + topic, newline: true)
%}}
```

## 先了解这几件事

| 你想做什么 | 从这里开始 |
| --- | --- |
| 写第一个提示词脚本 | [start](guide/start.md) |
| 了解 `{{% ... %}}` | [document](guide/document.md) |
| 选择嵌入或接管 | [mode](guide/mode.md) |
| 学习语法 | [language](language/index.md) |
| 读取 RimTalk 上下文 | [context](data/context.md) |
| 查询 RimWorld 数据 | [query](data/query.md) |
| 读取或管理 Expand Memory | [memory](memory/index.md) |
| 查找一个函数 | 从下方的函数索引进入 |
| 处理旧模板 | [art](compatibility/art.md) |
| 排查执行问题 | [error](guide/error.md) |
| 按场景组织提示词 | [examples](examples/index.md) |
| 使用正则处理文本 | [regex](regex.md) |

## 语言和模板的边界

Arti 与 RimTalk 中已有的两种模板语法并存：

| 写法 | 由谁处理 | 用途 |
| --- | --- | --- |
| `{{% ... %}}` | Arti | 条件、循环、函数、数据查询和明确输出 |
| `{{ ... }}` | RimTalk Scriban | 原有的 Scriban 模板 |
| `{{ art.xxx }}` | Advanced RimTalk 兼容层 | 旧版占位符和旧版函数 |

三种写法各自有明确的处理阶段。Arti 只识别 `{{% ... %}}`，普通 Scriban 和文本中的其他花括号按原有流程处理。

## 当前运行边界

- Arti 通过白名单入口读取宿主提供的数据。
- 游戏对象只开放明确的读取成员；文件、网络和程序集访问不在 Arti 的运行时接口中。
- 可选 [`memory`](memory/index.md) 模块提供 RimTalk - Expand Memory 的明确读写函数，调用写入函数会改动该 Mod 的记忆数据。
- `core.emit`、`core.emit_if` 和它们的别名负责输出。
- 每次执行都有步数上限和函数调用深度上限。
- 每个代码块拥有独立的局部作用域；需要跨代码块保存文本时使用 [`setvar`](global/setvar.md) 和 [`getvar`](global/getvar.md)。
- 嵌入模式保留 RimTalk 的提示词条目顺序、角色、历史消息和第三方注入内容。
- 接管模式只接管提示词组装，不接管 RimTalk 的触发和模型请求。

## 文档分类

### 使用指南

- [guide](guide/index.md)
- [start](guide/start.md)
- [document](guide/document.md)
- [mode](guide/mode.md)
- [settings](guide/settings.md)
- [runtime](guide/runtime.md)
- [error](guide/error.md)

### 语言

- [language](language/index.md)
- [comment](language/comment.md)
- [identifier](language/identifier.md)
- [string](language/string.md)
- [number](language/number.md)
- [array](language/array.md)
- [object](language/object.md)
- [literal](language/literal.md)
- [operator](language/operator.md)
- [scope](language/scope.md)
- [use](language/use.md)
- [optional](language/optional.md)
- [group](language/group.md)
- [let](language/let.md)
- [const](language/const.md)
- [assignment](language/assignment.md)
- [function](language/function.md)
- [method](language/method.md)
- [if](language/if.md)
- [else](language/else.md)
- [for](language/for.md)
- [while](language/while.md)
- [return](language/return.md)
- [break](language/break.md)
- [continue](language/continue.md)

### 全局函数

- [global](global/index.md)
- [range](global/range.md)
- [random](global/random.md)
- [getvar](global/getvar.md)
- [setvar](global/setvar.md)

### `core` 函数

- [core](core/index.md)
- [random](core/random.md)
- [value](core/value.md)
- [text](core/text.md)
- [escape](core/escape.md)
- [diag](core/diag.md)
- [emit](core/emit.md)
- [append](core/append.md)
- [emit_if](core/emit_if.md)
- [mod](core/mod.md)
- [packageid](core/packageid.md)
- [log](core/log.md)
- [warn](core/warn.md)
- [int](core/int.md)
- [float](core/float.md)
- [pick](core/pick.md)
- [default](core/default.md)
- [coalesce](core/coalesce.md)
- [join_nonempty](core/join_nonempty.md)
- [trim_lines](core/trim_lines.md)
- [indent](core/indent.md)
- [xml_text](core/xml_text.md)
- [json](core/json.md)
- [markdown](core/markdown.md)

### `core.string` 函数

- [string](core/string.md)
- [len](core/string/len.md)
- [remove_space](core/string/remove_space.md)
- [remove_spaces](core/string/remove_spaces.md)
- [append](core/string/append.md)
- [prepend](core/string/prepend.md)
- [upper](core/string/upper.md)
- [uppercase](core/string/uppercase.md)
- [lower](core/string/lower.md)
- [lowercase](core/string/lowercase.md)
- [capitalize](core/string/capitalize.md)
- [title](core/string/title.md)
- [trim](core/string/trim.md)
- [trim_start](core/string/trim_start.md)
- [trim_end](core/string/trim_end.md)
- [replace](core/string/replace.md)
- [contains](core/string/contains.md)
- [starts_with](core/string/starts_with.md)
- [ends_with](core/string/ends_with.md)
- [substring](core/string/substring.md)
- [split](core/string/split.md)
- [repeat](core/string/repeat.md)
- [is_empty](core/string/is_empty.md)

### 上下文和游戏数据

- [data](data/index.md)
- [context](data/context.md)
- [pawn](data/pawn.md)
- [recipient](data/recipient.md)
- [map](data/map.md)
- [chat](data/chat.md)
- [game](data/game.md)
- [current](data/current.md)
- [world](data/world.md)
- [find](data/find.md)
- [maps](data/maps.md)
- [pawns](data/pawns.md)
- [faction](data/faction.md)
- [factions](data/factions.md)
- [settlements](data/settlements.md)
- [sites](data/sites.md)
- [caravans](data/caravans.md)
- [world_objects](data/world_objects.md)
- [mods](data/mods.md)
- [defs](data/defs.md)
- [def](data/def.md)
- [thing](data/thing.md)
- [things](data/things.md)
- [cell](data/cell.md)
- [query](data/query.md)
- [read](data/read.md)
- [has](data/has.md)
- [keys](data/keys.md)

### 可选 `memory` 模块

- [memory](memory/index.md)
- [use](memory/use.md)
- [layer](memory/layer.md)
- [pawn](memory/pawn/index.md)
- [value](memory/value.md)
- [knowledge](memory/knowledge/index.md)
- [knowledge_value](memory/knowledge_value.md)
- [stats](memory/stats.md)

### 兼容

- [compatibility](compatibility/index.md)
- [art](compatibility/art.md)
- [scriban](compatibility/scriban.md)
- [random_int](compatibility/random_int.md)
- [random_float](compatibility/random_float.md)
- [choose](compatibility/choose.md)
- [join](compatibility/join.md)

### 实战示例

- [examples](examples/index.md)
- [context-filter](examples/context-filter.md)
- [optional-mod](examples/optional-mod.md)
- [raw-data](examples/raw-data.md)
- [regex](examples/regex.md)

### 文本处理

- [regex](regex.md)

## 文档约定

- `{{% ... %}}`：Arti 正式代码；
- `{{ ... }}`：RimTalk 原生 Scriban；
- `art.*`：旧版兼容占位符；
- `core.*`：Arti 内置模块；
- `ctx.*`、`pawn.*`、`map.*`：RimTalk 上下文或游戏数据。
