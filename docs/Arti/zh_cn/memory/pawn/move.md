# move

```text
memory.pawn.move(pawn?, id, layer)
```

把指定记忆移动到目标层。目标层可使用 `active`、`situational`、`event_log` 或 `archive` 及其别名。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.move(id, layer: "archive")
}
```

目标层名称见 [layer](../layer.md)。
