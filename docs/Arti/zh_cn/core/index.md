# core

`core` 是 Arti 的内置模块。它包含输出、随机数、文本整理、转义、模块状态和 RimWorld 数据查询。

## 函数

| 函数 | 用途 |
| --- | --- |
| [`emit`](emit.md) | 写入当前代码块输出 |
| [`append`](append.md) | `emit` 的别名 |
| [`emit_if`](emit_if.md) | 条件满足时写入 |
| [`mod`](mod.md) | 查询 Mod 状态 |
| [`packageid`](packageid.md) | `mod` 的兼容别名 |
| [`log`](log.md) | 写入开发日志，不输出文本 |
| [`warn`](warn.md) | `core.diag.warn`，写入警告回调 |
| [`int`](int.md) | 生成随机整数 |
| [`float`](float.md) | 生成随机小数 |
| [`pick`](pick.md) | 随机选择候选值 |
| [`default`](default.md) | 空值回退 |
| [`coalesce`](coalesce.md) | 取第一个非空值 |
| [`join_nonempty`](join_nonempty.md) | 忽略空项后拼接 |
| [`trim_lines`](trim_lines.md) | 清理多行文本 |
| [`indent`](indent.md) | 缩进多行文本 |
| [`xml_text`](xml_text.md) | 转义 XML 文本 |
| [`json`](json.md) | 转义 JSON 字符串内容 |
| [`markdown`](markdown.md) | 转义 Markdown 标点 |

## 命名空间

- [`random`](random.md)：随机整数、小数和候选值。
- [`value`](value.md)：空值回退。
- [`text`](text.md)：文本拼接和多行整理。
- [`escape`](escape.md)：XML、JSON 和 Markdown 转义。
- [`diag`](diag.md)：开发诊断。
- [`string`](string.md)：字符串函数，支持倒置调用。

## 数据命名空间

常用入口：

```arti
core.pawns.colonists
core.maps.home
core.mods.dlc.biotech_active
core.defs.find("ThingDef", "Silver")
core.query.things_at(map, 10, 20)
```

数据命名空间和选择器见 [query](../data/query.md) 以及各自的页面。

## 访问边界

`core` 的 RimWorld 数据入口是只读的，访问范围包括显式命名空间、选择器和公开字段/属性。`core.string` 提供列出的字符串函数和倒置调用；游戏对象的通用方法、文件、网络、程序集和 Unity 对象访问不属于这套接口。
