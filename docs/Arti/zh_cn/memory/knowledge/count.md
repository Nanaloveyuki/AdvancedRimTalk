# count

```text
memory.knowledge.count()
```

返回公共知识数量。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    core.emit(memory.knowledge.count())
}
```

返回值是整数；需要对象列表时使用 [all](all.md)、[find](find.md) 或 [find_content](find_content.md)。
