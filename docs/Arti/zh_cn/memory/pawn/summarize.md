# summarize

```text
memory.pawn.summarize(pawn?)
```

对活跃记忆执行手动汇总，成功时返回 `true`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.pawn.summarize()
}
```

返回 `true` 表示汇总流程已调用成功。
