# run_decay

```text
memory.pawn.run_decay(pawn?)
```

执行该 Pawn 记忆维护器的衰减流程，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.run_decay()
}
```

返回 `true` 表示维护流程已调用成功。
