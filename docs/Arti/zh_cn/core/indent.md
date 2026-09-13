# indent

`core.text.indent(text, level)` 给多行文本增加缩进。每一级缩进是两个空格：

```arti
let body = core.text.indent("第一行\n第二行", 1)
```

结果是：

```text
  第一行
  第二行
```

`level <= 0` 时原样返回文本。换行符会统一为 LF。
