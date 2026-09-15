# get

```text
memory.pawn.get(pawn?, id)
```

按 `id` 读取单条记忆。`id` 可以是当前 ID 或来源 ID；找不到时返回 `null`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let item = memory.pawn.get(id: 42)
    if item != null {
        core.emit(item.content)
    }
}
```

返回对象字段见 [value](../value.md)。
