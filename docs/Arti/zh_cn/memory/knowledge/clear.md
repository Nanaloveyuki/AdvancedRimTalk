# clear

```text
memory.knowledge.clear()
```

清空公共知识库，成功时返回 `true`。这个操作影响范围很大，通常只用于明确的维护脚本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.clear()
}
```

返回 `true` 表示清空成功。
