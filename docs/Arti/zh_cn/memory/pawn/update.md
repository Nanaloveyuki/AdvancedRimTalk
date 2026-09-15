# update

```text
memory.pawn.update(pawn?, id, content?, importance?, activity?, pinned?, notes?, layer?, type?, tags?, keywords?)
```

更新指定记忆。可更新正文、重要度、活动度、固定状态、备注、层、类型、标签和关键词。成功修改至少一个字段时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.update(
        id: id,
        content: "新的记忆内容",
        importance: 0.7,
        layer: "event_log"
    )
}
```

可更新字段见 [value](../value.md)；层名称见 [layer](../layer.md)。
