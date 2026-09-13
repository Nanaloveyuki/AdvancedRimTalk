# text

`core.text` 提供文本组合和多行整理函数：

- [`join_nonempty`](join_nonempty.md)：跳过空项后拼接；
- [`trim_lines`](trim_lines.md)：清理每行空白和连续空行；
- [`indent`](indent.md)：按级别增加两个空格一级的缩进。

```arti
let body = core.text.indent(
    core.text.trim_lines(raw_text),
    1,
)
core.emit(body)
```
