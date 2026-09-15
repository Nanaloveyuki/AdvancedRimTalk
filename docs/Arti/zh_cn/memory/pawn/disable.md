# disable

```text
memory.pawn.disable(pawn?, id)
```

把指定记忆的活动度设为 `0`，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.disable(id)
}
```

返回 `true` 表示更新成功。
