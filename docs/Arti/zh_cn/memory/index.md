# memory

`memory` 是 Arti 对 RimTalk - Expand Memory 的可选桥接模块。模块可用后，脚本可以读取 Pawn 记忆、整理相关记忆、管理记忆层，并访问公共知识库。

## 推荐导入

```arti
use optional memory as memory

if memory.api_available {
    core.emit(memory.pawn.context(count: 5), newline: true)
}
```

`memory` 是 `cj.rimtalk.expandmemory` 的简写。需要写全包 ID 时使用：

```arti
use optional cj.rimtalk.expandmemory as memory
```

`memory` 依赖 RimTalk - Expand Memory。用 `optional` 可以让同一份提示词在未安装该 Mod 的环境中继续运行；访问 `memory.pawn` 或 `memory.knowledge` 前先检查 `memory.api_available`。

## 模块入口

- [use](use.md)：导入、状态和兼容边界。
- [layer](layer.md)：四层记忆和别名。
- [pawn](pawn/index.md)：Pawn 记忆函数。
- [value](value.md)：单条 Pawn 记忆对象。
- [knowledge](knowledge/index.md)：公共知识库函数。
- [knowledge_value](knowledge_value.md)：单条公共知识对象。
- [stats](stats.md)：公共知识统计对象。

## Pawn 记忆函数

- [list](pawn/list.md)
- [all](pawn/all.md)
- [count](pawn/count.md)
- [get](pawn/get.md)
- [add](pawn/add.md)
- [update](pawn/update.md)
- [remove](pawn/remove.md)
- [delete](pawn/delete.md)
- [pin](pawn/pin.md)
- [unpin](pawn/unpin.md)
- [enable](pawn/enable.md)
- [disable](pawn/disable.md)
- [move](pawn/move.md)
- [context](pawn/context.md)
- [relevant](pawn/relevant.md)
- [retrieve](pawn/retrieve.md)
- [combined](pawn/combined.md)
- [memory](pawn/memory.md)
- [run_decay](pawn/run_decay.md)
- [cleanup](pawn/cleanup.md)
- [enforce_limits](pawn/enforce_limits.md)
- [summarize](pawn/summarize.md)
- [archive](pawn/archive.md)

## 公共知识函数

- [add](knowledge/add.md)
- [add_ex](knowledge/add_ex.md)
- [add_batch](knowledge/add_batch.md)
- [get](knowledge/get.md)
- [find](knowledge/find.md)
- [find_tag](knowledge/find_tag.md)
- [find_content](knowledge/find_content.md)
- [all](knowledge/all.md)
- [count](knowledge/count.md)
- [exists](knowledge/exists.md)
- [update](knowledge/update.md)
- [update_content](knowledge/update_content.md)
- [update_tag](knowledge/update_tag.md)
- [update_importance](knowledge/update_importance.md)
- [set_enabled](knowledge/set_enabled.md)
- [enable](knowledge/enable.md)
- [disable](knowledge/disable.md)
- [remove](knowledge/remove.md)
- [delete](knowledge/delete.md)
- [remove_by_tag](knowledge/remove_by_tag.md)
- [clear](knowledge/clear.md)
- [import](knowledge/import.md)
- [export](knowledge/export.md)
- [stats](knowledge/stats.md)
- [inject](knowledge/inject.md)

## 写入边界

Pawn 记忆和公共知识函数包含写入操作，例如 `add`、`update`、`remove`、`pin`、`move`、`clear`。这些调用会改动 RimTalk - Expand Memory 的数据；只想给模型补充上下文时，优先使用 `context`、`relevant`、`retrieve`、`combined`、`find` 和 `inject`。
