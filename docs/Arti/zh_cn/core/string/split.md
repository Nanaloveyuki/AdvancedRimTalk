# split

`split(value, separator)` 按分隔符切分文本，返回数组。

## 语法

```arti
let parts = split("Alice,Bob", ",")
let parts2 = core.string.split("Alice,Bob", ",")
let parts3 = "Alice,Bob".split(",")

core.emit(parts[0] + "|" + parts[1])
```

输出 `Alice|Bob`。

分隔符为空文本时，函数按字符切分。普通分隔符产生的空项会保留：

```arti
let parts = "a,,b".split(",")
core.emit(parts[1].is_empty())
```

数组项可以继续使用字符串函数。

