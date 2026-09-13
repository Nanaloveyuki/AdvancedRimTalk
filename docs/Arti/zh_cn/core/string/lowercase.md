# lowercase

`lowercase` 是 [`lower`](lower.md) 的别名，把文本中的字母转换为小写。

## 语法

```arti
let a = lowercase("HELLO")
let b = core.string.lowercase("HELLO")
let c = "HELLO".lowercase()

core.emit(a + "|" + b + "|" + c)
```

三种写法都会返回 `hello`。新代码也可以使用名称更短的 `lower`。

