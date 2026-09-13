# append

`core.string.append(value, suffix)` 把 `suffix` 追加到 `value` 末尾。

## 语法

```arti
let text = append("角色：", "Alice")
let text2 = core.string.append("角色：", "Alice")
let text3 = "角色：".append("Alice")
```

三种写法都会返回 `角色：Alice`。倒置调用时，接收字符串对应 `value` 参数。

## 命名参数

```arti
let text = "角色：".append(suffix: "Alice")
core.emit(text)
```

这个函数只组合文本，不写入提示词。需要写入当前代码块时，使用 [`core.emit`](../emit.md)。

