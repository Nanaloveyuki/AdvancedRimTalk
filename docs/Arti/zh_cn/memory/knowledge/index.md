# knowledge

`memory.knowledge` 管理 RimTalk - Expand Memory 的公共知识库。`memory.common_knowledge` 是同一个入口。

```arti
use optional memory as memory

if memory.api_available {
    let results = memory.knowledge.find_content("贸易")
    for item in results {
        core.emit("- " + item.content, newline: true)
    }
}
```

## 函数

| 函数 | 结果 |
| --- | --- |
| [add](add.md) | 新增公共知识 |
| [add_ex](add_ex.md) | 新增扩展公共知识 |
| [add_batch](add_batch.md) | 批量新增公共知识 |
| [get](get.md) | 按 ID 读取知识对象 |
| [find](find.md) / [find_tag](find_tag.md) | 按标签查找 |
| [find_content](find_content.md) | 按内容查找 |
| [all](all.md) | 返回全部知识对象 |
| [count](count.md) | 返回知识数量 |
| [exists](exists.md) | 判断 ID 是否存在 |
| [update](update.md) | 更新多个字段 |
| [update_content](update_content.md) | 更新正文 |
| [update_tag](update_tag.md) | 更新标签 |
| [update_importance](update_importance.md) | 更新重要度 |
| [set_enabled](set_enabled.md) / [enable](enable.md) / [disable](disable.md) | 设置启用状态 |
| [remove](remove.md) / [delete](delete.md) | 删除指定 ID |
| [remove_by_tag](remove_by_tag.md) | 删除指定标签的知识 |
| [clear](clear.md) | 清空公共知识库 |
| [import](import.md) | 从文本导入 |
| [export](export.md) | 导出为文本 |
| [stats](stats.md) | 返回统计对象 |
| [inject](inject.md) | 把匹配知识注入一段上下文文本 |

单条公共知识对象字段见 [knowledge_value](../knowledge_value.md)。
