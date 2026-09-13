# replace

`replace(value, old, replacement)` 把 `value` 中所有匹配 `old` 的片段替换为 `replacement`。匹配区分大小写。

## 语法

```arti
let text = replace("a-b-a", "-", "_")
let text2 = core.string.replace("a-b-a", "-", "_")
let text3 = "a-b-a".replace("-", "_")
core.emit(text)
```

三种写法都会返回 `a_b_a`。

## 命名参数

```arti
let text = "a-b".replace(old: "-", replacement: "_")
core.emit(text)
```

