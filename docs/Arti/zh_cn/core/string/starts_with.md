# starts_with

`starts_with(value, part)` 判断文本是否以指定片段开头，返回布尔值。匹配区分大小写。

## 语法

```arti
let result = "RimWorld".starts_with("Rim")
if result {
    core.emit("这是 RimWorld")
}
```

普通调用和命名空间调用分别是 `starts_with(value, part)` 与 `core.string.starts_with(value, part)`。

