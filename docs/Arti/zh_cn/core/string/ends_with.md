# ends_with

`ends_with(value, part)` 判断文本是否以指定片段结尾，返回布尔值。匹配区分大小写。

## 语法

```arti
let result = "RimWorld".ends_with("World")
if result {
    core.emit("名称以 World 结尾")
}
```

普通调用和命名空间调用分别是 `ends_with(value, part)` 与 `core.string.ends_with(value, part)`。

