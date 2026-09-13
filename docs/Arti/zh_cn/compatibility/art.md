# art

`art.*` 是 Advanced RimTalk 保留的旧模板兼容层，主要用于让已有 RimTalk 模板继续工作。正式 Arti 使用 `{{% ... %}}`。

旧写法使用普通 Scriban 花括号：

```text
{{ art.pawn_name }}
{{ art.random_int(1, 6) }}
```

新代码应优先使用正式 Arti：

```text
{{%
core.emit(pawn.name)
%}}
```

## 可用占位符

常用旧占位符包括：

```text
art.pawn_name
art.recipient_name
art.talk_type
art.dialogue_type
art.intent
art.topic
art.status
art.prompt
art.raw_prompt
art.context
art.pawn_context
art.tick
art.hour
art.date
art.season
art.weather
art.temperature
art.wealth
art.is_announcement
art.is_monologue
art.state
```

它们读取当前 Prompt 快照。不存在的值通常会展开为空文本。

## 旧函数

| 函数 | 用途 |
| --- | --- |
| `art.random_int(min, max)` | 包含两端的随机整数 |
| `art.random_float(min, max)` | 随机小数 |
| `art.choose(value, ...)` | 从参数中选择一个 |
| `art.default(value, fallback)` | 空值回退 |
| `art.coalesce(value, ...)` | 取第一个非空值 |
| `art.join(separator, value, ...)` | 拼接参数 |

函数可以写成单行或跨行调用，也支持常见的空白参数写法：

```text
{{ art.choose(
    "calm",
    "urgent",
    "curious"
) }}
```

具体函数见 [random_int](random_int.md)、[random_float](random_float.md)、[choose](choose.md)、[default](../core/default.md)、[coalesce](../core/coalesce.md) 和 [join](join.md)。

## 处理顺序

在嵌入模式中，Advanced RimTalk 会：

1. 先执行正式 `{{% ... %}}` Arti；
2. 再展开 `art.*`；
3. 最后交给 RimTalk 原生 Scriban。

旧占位符展开出来的文本会受到保护，后续 Scriban 阶段会把它作为普通文本处理。

接管模式使用独立的接管文档。预设中的 `art.*` 需要迁移到该文档，并改写为正式 Arti。
