# memory

```text
memory.pawn.memory(pawn?)
```

`memory` 是 `combined` 的别名，返回组合记忆文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    core.emit(memory.pawn.memory(), newline: true)
}
```

返回值是文本；需要结构化对象列表时使用 [list](list.md) 或 [retrieve](retrieve.md)。
