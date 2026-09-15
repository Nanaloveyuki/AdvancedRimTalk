# pin

```text
memory.pawn.pin(pawn?, id)
```

固定指定记忆，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.pin(id)
}
```

返回 `true` 表示固定成功。
