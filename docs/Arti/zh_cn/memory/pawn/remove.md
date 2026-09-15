# remove

```text
memory.pawn.remove(pawn?, id)
```

删除指定记忆，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    if memory.pawn.remove(id) {
        core.emit("已删除")
    }
}
```

返回 `true` 表示删除成功。
