# 开始

这页带你从一个最小示例开始，逐步写出一个可用的 Arti 提示词。

## 准备工作

1. 安装并启用 Advanced RimTalk。
2. 确认 RimTalk 已正常工作。
3. 打开 RimTalk 的提示词预设或提示词条目编辑位置。
4. 默认先使用**嵌入模式**。它保留 RimTalk 原有的提示词组装方式。

如果你需要让 Advanced RimTalk 自己组装 system/user 消息，再阅读 [mode](mode.md)。

## 在编辑器中查文档

Arti Editor 的“文档”按钮会打开独立窗口，可以拖动、缩放，并与提示词并排查看。在 `{{% ... %}}` 代码块内按住 **Control**，左键点击关键词，即可打开对应的索引文档；已打开的文档窗口会继续使用，不会重复创建。

支持语言关键词、内置函数和命名空间，以及 RimTalk 或其他 Mod 注册的变量。例如，点击 `core.emit` 中的 `emit` 会打开输出函数说明；`core.string.append` 和 `memory.pawn.context` 则分别打开字符串函数与记忆上下文说明。按住 Control 悬停时，可跳转的词会显示下划线和提示。

玩家声明的变量、函数、常量和参数不参与查找，即使与内置名称同名也不会跳转。注释、字符串正文、原生 Scriban 和 Markdown 示例同样不参与；Arti 插值字符串中的表达式可以查文档。普通左键仍用于编辑，查文档不会执行提示词。没有 IrisMenus 时，窗口显示可选中、可复制的 Markdown 源文。

## 第一个代码块

```text
当前对话角色：
{{%
core.emit(pawn.name, newline: true)
%}}
```

`{{%` 和 `%}}` 是 Arti 的边界。`emit` 把值写入当前提示词，`newline: true` 在值后追加换行。

如果当前没有 Pawn，`pawn.name` 可能为空。更稳妥的写法是给它准备回退值：

```text
{{%
let name = "未知角色"
if pawn != null {
    name = core.value.default(pawn.name, "未知角色")
}
core.emit(name, newline: true)
%}}
```

## 加入判断

```text
{{%
if pawn.mood != null {
    core.emit("当前心情：" + pawn.mood, newline: true)
} else {
    core.emit("当前没有可用的心情信息。", newline: true)
}
%}}
```

Arti 使用 `null` 表示没有值。空字符串和只有空白的字符串在 [`default`](../core/default.md) 与 [`coalesce`](../core/coalesce.md) 中也会被视为空。

## 加入循环

```text
殖民者：
{{%
for colonist in core.pawns.colonists {
    core.emit("- " + colonist.name, newline: true)
}
%}}
```

`for` 遍历一个集合。没有地图或没有符合条件的 Pawn 时，集合为空，循环体没有输出。

也可以用 [`range`](../global/range.md) 生成数字：

```text
{{%
for index in range(3) {
    core.emit("候选方案 " + index, newline: true)
}
%}}
```

这会输出 `0`、`1`、`2` 三行。

## 组合成一个小模板

```text
请根据下面的上下文回答，只使用其中提供的信息。

角色：{{%
let name = "未知角色"
if pawn != null {
    name = core.value.default(pawn.name, "未知角色")
}
core.emit(name)
%}}

话题：{{%
core.emit(core.value.default(ctx.topic, "未指定"))
%}}

附近的殖民者：
{{%
for colonist in core.pawns.colonists {
    core.emit("- " + colonist.name, newline: true)
}
%}}
```

每个代码块都在生成当前提示词时执行，并拥有自己的局部变量。需要跨块保存文本时，请看 [setvar](../global/setvar.md)。

## 下一步

- 想系统学习语法：看 [syntax](../language/index.md)。
- 想查某个上下文成员：看 [context](../data/context.md) 或 [pawn](../data/pawn.md)。
- 想查询地图、Pawn、Defs：看 [query](../data/query.md)。
- 想格式化输出：看 [join_nonempty](../core/join_nonempty.md)、[trim_lines](../core/trim_lines.md) 和 [indent](../core/indent.md)。
- 看见错误标记：看 [error](error.md)。
