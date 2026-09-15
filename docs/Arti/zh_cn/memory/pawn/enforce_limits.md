# enforce_limits

```text
memory.pawn.enforce_limits(pawn?)
```

应用 Expand Memory 的记忆数量限制，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.enforce_limits()
}
```

返回 `true` 表示限制应用流程已调用成功。
