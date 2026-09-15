# pawn

`memory.pawn` 管理某个 Pawn 的四层记忆。未显式传入 Pawn 时，函数使用当前对话 Pawn。

```arti
use optional memory as memory

if memory.api_available {
    let count = memory.pawn.count()
    core.emit("记忆数量：" + count)
}
```

## Pawn 参数

多数函数可以使用当前 Pawn，也可以传入 Pawn 对象或名称：

```arti
memory.pawn.list()
memory.pawn.list(recipient)
memory.pawn.list(pawn: recipient)
memory.pawn.list(pawn: "Alice")
```

字符串按 `ThingID` 或 `LabelShort` 匹配当前请求中的 Pawn 集合。

## 函数

| 函数 | 结果 |
| --- | --- |
| [list](list.md) / [all](all.md) | 返回全部层的记忆对象 |
| [count](count.md) | 返回记忆数量 |
| [get](get.md) | 按 ID 读取单条记忆 |
| [add](add.md) | 新增记忆并返回 ID |
| [update](update.md) | 更新记忆内容或元数据 |
| [remove](remove.md) / [delete](delete.md) | 删除记忆 |
| [pin](pin.md) / [unpin](unpin.md) | 固定或取消固定 |
| [enable](enable.md) / [disable](disable.md) | 设置活动度为 1 或 0 |
| [move](move.md) | 移动到指定层 |
| [context](context.md) | 生成 Expand Memory 上下文文本 |
| [relevant](relevant.md) | 返回相关记忆对象列表 |
| [retrieve](retrieve.md) | 使用查询条件检索记忆 |
| [combined](combined.md) / [memory](memory.md) | 返回 RimTalk 变量提供器的组合记忆文本 |
| [run_decay](run_decay.md) | 执行衰减维护 |
| [cleanup](cleanup.md) | 清理低活动度记忆 |
| [enforce_limits](enforce_limits.md) | 应用记忆数量限制 |
| [summarize](summarize.md) | 手动汇总活跃记忆 |
| [archive](archive.md) | 归档事件日志 |

单条记忆对象字段见 [value](../value.md)。
