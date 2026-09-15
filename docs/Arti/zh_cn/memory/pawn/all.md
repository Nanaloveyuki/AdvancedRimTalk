# all

```text
memory.pawn.all(pawn?)
```

`all` 是 `list` 的别名，返回指定 Pawn 的全部层记忆对象列表。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let items = memory.pawn.all(pawn: recipient)
    core.emit("记忆数：" + items.count)
}
```

返回元素字段见 [value](../value.md)。`all` 与 [list](list.md) 使用同一套 Pawn 参数。
