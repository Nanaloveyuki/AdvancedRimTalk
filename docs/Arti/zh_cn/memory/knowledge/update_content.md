# update_content

```text
memory.knowledge.update_content(id, content)
```

更新指定公共知识的正文。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    memory.knowledge.update_content(id, "新的正文")
}
```

返回 `true` 表示正文更新成功。
