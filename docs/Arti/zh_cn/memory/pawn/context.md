# context

```text
memory.pawn.context(pawn?, count: 5)
```

调用 Expand Memory 的上下文生成逻辑，返回适合写入提示词的记忆文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let text = memory.pawn.context(count: 5)
    if text != "" {
        core.emit(text, newline: true)
    }
}
```

返回值是文本。需要结构化对象列表时使用 [relevant](relevant.md) 或 [retrieve](retrieve.md)。
