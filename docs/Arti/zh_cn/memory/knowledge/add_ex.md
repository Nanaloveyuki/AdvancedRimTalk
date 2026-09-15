# add_ex

```text
memory.knowledge.add_ex(tag, content, importance: 0.5, match_mode: "any", target_pawn_id: -1, can_be_extracted: false, can_be_matched: false)
```

新增扩展公共知识。它可以指定匹配模式、目标 Pawn 和扩展标记。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let id = memory.knowledge.add_ex(
        "rule",
        "回答贸易问题时先考虑库存。",
        importance: 0.9,
        match_mode: "any",
        can_be_matched: true
    )
}
```

返回 ID 可用于 [get](get.md)、[update](update.md) 和 [remove](remove.md)。
