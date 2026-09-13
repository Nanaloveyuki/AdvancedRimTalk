# prepend

`prepend(value, prefix)` 把 `prefix` 添加到 `value` 开头。

## 语法

```arti
let text = prepend("Alice", "角色：")
let text2 = core.string.prepend("Alice", "角色：")
let text3 = "Alice".prepend("角色：")
```

三种写法都会返回 `角色：Alice`。

## 命名参数

```arti
let text = "Alice".prepend(prefix: "角色：")
core.emit(text)
```

