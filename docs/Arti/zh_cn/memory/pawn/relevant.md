# relevant

```text
memory.pawn.relevant(pawn?, count: 5)
```

返回 Expand Memory 认为与当前情境相关的记忆对象列表。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    for item in memory.pawn.relevant(count: 3) {
        core.emit(item.content, newline: true)
    }
}
```

返回元素字段见 [value](../value.md)。
