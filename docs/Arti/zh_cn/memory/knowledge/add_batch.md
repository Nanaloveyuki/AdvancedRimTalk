# add_batch

```text
memory.knowledge.add_batch(entries, importance: 0.5)
```

批量新增公共知识并返回成功数量。`entries` 可以是数组或对象集合，对象字段使用 `tag` 和 `content`。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let added = memory.knowledge.add_batch([
        { tag: "base", content: "基地在温带森林。" },
        { tag: "trade", content: "白银优先用于药品。" }
    ])
}
```

返回值是成功新增的条目数；新增后可通过 [find](find.md) 或 [all](all.md) 读取。
