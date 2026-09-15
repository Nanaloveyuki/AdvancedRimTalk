# combined

```text
memory.pawn.combined(pawn?)
```

返回 Expand Memory 变量提供器生成的组合记忆文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    core.emit(memory.pawn.combined(), newline: true)
}
```

返回值是文本；`memory` 是同一功能的别名。
