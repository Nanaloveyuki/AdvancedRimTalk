# cleanup

```text
memory.pawn.cleanup(pawn?)
```

清理低活动度记忆，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.cleanup()
}
```

返回 `true` 表示清理流程已调用成功。
