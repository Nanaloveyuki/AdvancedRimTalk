# capitalize

`capitalize(value)` 把文本的第一个字符转换为大写，其余内容保持原样。

## 语法

```arti
let text = capitalize("hello world")
let text2 = core.string.capitalize("hello world")
let text3 = "hello world".capitalize()
core.emit(text)
```

结果是 `Hello world`。空文本返回空文本。

