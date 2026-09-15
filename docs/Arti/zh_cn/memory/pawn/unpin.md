# unpin

```text
memory.pawn.unpin(pawn?, id)
```

取消固定指定记忆，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.unpin(id)
}
```

返回 `true` 表示取消固定成功。
