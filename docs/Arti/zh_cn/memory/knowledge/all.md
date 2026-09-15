# all

```text
memory.knowledge.all()
```

返回公共知识库中的全部知识对象。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    core.emit("公共知识数量：" + memory.knowledge.all().count)
}
```

返回元素字段见 [knowledge_value](../knowledge_value.md)。
