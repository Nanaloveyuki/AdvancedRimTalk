# uppercase

`uppercase` 是 [`upper`](upper.md) 的别名，把文本中的字母转换为大写。

## 语法

```arti
let a = uppercase("hello")
let b = core.string.uppercase("hello")
let c = "hello".uppercase()

core.emit(a + "|" + b + "|" + c)
```

三种写法都会返回 `HELLO`。新代码也可以使用名称更短的 `upper`。

