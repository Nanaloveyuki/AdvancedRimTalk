# export

```text
memory.knowledge.export()
```

把公共知识库导出为文本。

## 示例

```arti
use optional memory as memory

if memory.api_available {
    let text = memory.knowledge.export()
    core.emit(text)
}
```

返回值是可保存或再次导入的文本。
