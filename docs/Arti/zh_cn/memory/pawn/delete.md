# delete

```text
memory.pawn.delete(pawn?, id)
```

`delete` 是 `remove` 的别名，删除指定记忆。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.delete(id: id)
}
```

返回 `true` 表示删除成功。
