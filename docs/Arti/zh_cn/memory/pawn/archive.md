# archive

```text
memory.pawn.archive(pawn?)
```

把事件日志交给 Expand Memory 归档流程，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.archive()
}
```

返回 `true` 表示归档流程已调用成功。
