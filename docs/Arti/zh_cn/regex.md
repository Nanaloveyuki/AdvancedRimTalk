# 正则表达式

Advanced RimTalk 的响应过滤设置使用 .NET 正则表达式语法。Arti 当前通过字符串函数完成脚本内筛选；需要按回复内容拦截或放行时，在响应设置中启用正则白名单、黑名单或忽略规则。

## 基本写法

```arti
let raw = core.value.default(ctx.topic, "")
let found = core.string.contains(raw, "食物")
core.emit_if(found, "话题涉及食物。", newline: true)
```

脚本内可先用 `contains`、`starts_with`、`split` 等函数完成确定性筛选：

```arti
let raw = core.value.default(ctx.topic, "")
if core.string.contains(raw, "food:") {
    core.emit("包含 food 字段。", newline: true)
}
```

## 常用模式

| 目标 | 模式 |
| --- | --- |
| 忽略大小写 | `(?i)word` |
| 数字 | `\\d+` |
| 非空白片段 | `\\S+` |
| 捕获字段 | `key\\s*:\\s*(.+)` |
| 行首/行尾 | `^...$` |

先用 `core.string.trim` 清理输入，再匹配；将捕获结果逐项输出或保存，便于检查原始数据格式。
