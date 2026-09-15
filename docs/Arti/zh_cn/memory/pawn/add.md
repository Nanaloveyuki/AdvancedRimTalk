# add

```text
memory.pawn.add(pawn?, content, layer: "active", type: "conversation", importance: 0.5, related_pawn: "", pinned: false, notes: "", tags: null, keywords: null)
```

新增一条 Pawn 记忆并返回新 ID。失败时返回 `0`。`importance` 会限制在 `0..1`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let id = memory.pawn.add(
        "Alice 记住了这次交易。",
        layer: "active",
        type: "event",
        importance: 0.8,
        tags: ["trade"]
    )
}
```

返回 ID 可用于 [get](get.md)、[update](update.md) 和 [remove](remove.md)。层名称见 [layer](../layer.md)。
