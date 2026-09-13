# document

Arti 代码写在提示词文档中的正式代码块里：

```text
{{%
// 这里是 Arti
core.emit("hello")
%}}
```

正式代码块的开始标记是 `{{%`，结束标记是 `%}}`。Arti 只解析这两个标记之间的内容，其他文本按原文保留。

## 一个代码块做什么

代码块会在当前提示词生成时执行，并把 `emit` 写出的文本替换到原来的位置：

```text
状态：
{{%
let value = "未知"
if pawn != null {
    value = core.value.default(pawn.health, "未知")
}
core.emit(value)
%}}
```

如果 `pawn.health` 是 `"健康"`，生成的文本就是：

```text
状态：
健康
```

表达式语句只负责计算：

```text
{{%
pawn.name
%}}
```

上面的代码会计算 `pawn.name`，输出则由 [`emit`](../core/emit.md) 完成：

```text
{{%
core.emit(pawn.name)
%}}
```

## 代码块之间的变量

同一个提示词中可以有多个 Arti 代码块，但每个代码块都有自己的局部变量作用域：

```text
{{%
let name = pawn.name
%}}

{{%
// name 在这里不可用
core.emit(name)
%}}
```

如果需要跨代码块保存文本，使用 [`setvar`](../global/setvar.md)：

```text
{{%
setvar("my_name", pawn.name)
%}}

{{%
core.emit(getvar("my_name"))
%}}
```

会话变量以文本保存，不适合保存可继续访问的 Pawn、Map 或集合对象。

## Markdown、代码围栏和 Scriban

Arti 只在提示词正文中识别正式代码块。以下位置保留为示例文字：

- Markdown 代码围栏，例如三反引号或三波浪线围起来的内容；
- Markdown 行内代码，例如 `` `{{% ... %}}` ``；
- RimTalk 原生 Scriban 代码块，例如 `{{ pawn.name }}`。

下面的代码位于 Markdown 代码围栏中，仅展示写法：

````markdown
```arti
{{%
core.emit("example")
%}}
```
````

需要执行时，把代码放在实际的 RimTalk 提示词条目正文中。

## 未闭合代码块

如果写了 `{{%` 却没有对应的 `%}}`，Arti 会报告解析错误，并在生成结果中留下可见的错误标记。先修复代码块边界，再检查后续语法。

## 与普通花括号的区别

这两种写法用途不同：

```text
{{% core.emit(pawn.name) %}}   // Arti
{{ pawn.name }}           // RimTalk Scriban
```

Arti 先处理正式代码块，然后由嵌入模式继续处理旧占位符和 RimTalk 的 Scriban。接管模式只执行你配置的接管文档中的 Arti 代码，详情见 [mode](mode.md)。
