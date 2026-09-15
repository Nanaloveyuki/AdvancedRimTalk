# stats

```text
memory.knowledge.stats()
```

返回公共知识统计对象。字段见 [stats](../stats.md)。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let stats = memory.knowledge.stats()
    core.emit(stats.enabled_count + "/" + stats.total_count)
}
```

返回字段见 [stats](../stats.md)。
