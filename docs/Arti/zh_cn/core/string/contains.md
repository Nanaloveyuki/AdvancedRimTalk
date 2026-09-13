# contains

`contains(value, part)` 判断文本是否包含指定片段，返回布尔值。匹配区分大小写。

## 语法

```arti
let found = contains("RimWorld", "World")
let found2 = core.string.contains("RimWorld", "World")
let found3 = "RimWorld".contains("World")

if found3 {
    core.emit("找到")
}
```

