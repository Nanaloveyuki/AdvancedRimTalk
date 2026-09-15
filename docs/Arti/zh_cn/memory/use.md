# use

`memory` 模块用于访问 RimTalk - Expand Memory。推荐按可选模块导入：

```arti
use optional memory as memory

if memory.api_available {
    core.emit("Expand Memory 已可用", newline: true)
}
```

## 包 ID

| 写法 | 含义 |
| --- | --- |
| `use optional memory as memory` | 简写形式 |
| `use optional cj.rimtalk.expandmemory as memory` | 完整包 ID |

两个写法都指向 RimTalk - Expand Memory。

## 状态字段

模块可用时提供：

| 字段 | 含义 |
| --- | --- |
| `installed` | RimTalk - Expand Memory 已安装 |
| `active` | Mod 当前启用 |
| `available` / `api_available` | 反射桥接已找到需要的 API |
| `package_id` | 规范包 ID：`cj.rimtalk.expandmemory` |
| `version` | 安装版本文本 |

`memory.api_available` 为真时，`memory.pawn`、`memory.knowledge` 和各层命名空间可用。

## 典型用法

```arti
use optional memory as memory

if memory.api_available {
    let lines = memory.pawn.combined()
    if lines != "" {
        core.emit("相关记忆：", newline: true)
        core.emit(lines, newline: true)
    }
}
```
