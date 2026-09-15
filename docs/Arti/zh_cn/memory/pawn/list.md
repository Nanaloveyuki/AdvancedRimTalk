# list

```text
memory.pawn.list(pawn?)
```

返回指定 Pawn 的全部层记忆对象列表。省略 Pawn 时使用当前对话 Pawn。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    for item in memory.pawn.list() {
        core.emit("- " + item.content, newline: true)
    }
}
```

返回元素字段见 [value](../value.md)。需要限制层时，可以改用 [layer](../layer.md) 中的层命名空间。
