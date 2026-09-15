# import

```text
memory.knowledge.import(text, clear_existing: false)
```

从文本导入公共知识，返回导入数量。`clear_existing` 为真时先清空现有知识。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let count = memory.knowledge.import(text, clear_existing: false)
}
```

返回值是成功导入的条目数。
