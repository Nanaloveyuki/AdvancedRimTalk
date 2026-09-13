# escape

`core.escape` 提供三种面向文本的转义函数：

- [`xml_text`](xml_text.md)：XML 文本内容；
- [`json`](json.md)：JSON 字符串内容，不含外层引号；
- [`markdown`](markdown.md)：选定的 Markdown 标点。

这些函数接收文本片段并返回转义后的结果；完整的 XML、JSON 或 Markdown 结构由调用者组织。
