# remove_spaces

`remove_spaces` 是 [`remove_space`](remove_space.md) 的别名，删除文本中的所有空白字符。

## 语法

```arti
let a = remove_spaces("a b")
let b = core.string.remove_spaces("a b")
let c = "a b".remove_spaces()

core.emit(a + "|" + b + "|" + c)
```

三种写法都会返回 `ab`。新代码也可以使用名称更短的 `remove_space`。

