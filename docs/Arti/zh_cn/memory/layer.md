# layer

RimTalk - Expand Memory 使用四层 Pawn 记忆。Arti 在 `memory` 根模块和 `memory.pawn` 下都提供层命名空间。

| 层 | 可用名称 | 说明 |
| --- | --- | --- |
| Active | `active` / `abm` | 当前活跃记忆 |
| Situational | `situational` / `short_term` / `scm` | 情境或短期记忆 |
| EventLog | `event_log` / `mid_term` / `els` | 事件日志或中期记忆 |
| Archive | `archive` / `long_term` / `clpa` | 归档或长期记忆 |

## 示例

```arti
use optional memory as memory

if memory.api_available {
    for item in memory.active.list() {
        core.emit("- " + item.content, newline: true)
    }
}
```

层命名空间提供 `list`、`all`、`count`、`get`、`add`、`update`、`remove`、`delete`、`pin`、`unpin`、`enable`、`disable` 和 `move`。这些函数与 [pawn](pawn/index.md) 页面中的同名函数一致，默认层改为当前命名空间对应的层。

```arti
memory.event_log.add("发生了一次袭击", type: "event", importance: 0.8)
memory.archive.move(id, layer: "active")
```
