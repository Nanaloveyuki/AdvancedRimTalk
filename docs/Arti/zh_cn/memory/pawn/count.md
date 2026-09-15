# count

```text
memory.pawn.count(pawn?)
```

返回指定 Pawn 的记忆数量。省略 Pawn 时统计当前对话 Pawn。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    core.emit("记忆数量：" + memory.pawn.count())
}
```

需要对象列表时使用 [list](list.md) 或 [retrieve](retrieve.md)。
