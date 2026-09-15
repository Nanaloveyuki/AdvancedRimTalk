# retrieve

```text
memory.pawn.retrieve(pawn?, related_pawn?, max_count: 10, include_context: true, layer?, type?, tags?, keywords?)
```

按条件检索记忆对象。建议使用命名参数表达筛选条件。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let items = memory.pawn.retrieve(
        layer: "event_log",
        type: "event",
        tags: ["raid"],
        max_count: 5
    )
}
```

返回元素字段见 [value](../value.md)。层名称和别名见 [layer](../layer.md)。
