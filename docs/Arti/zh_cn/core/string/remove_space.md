# remove_space

`remove_space(value)` 删除文本中的所有空白字符，包括空格、制表符和换行。

## 语法

```arti
let compact = remove_space("a b\nc")
let compact2 = "a b\nc".remove_space()
core.emit(compact)
```

结果是 `abc`。`remove_spaces` 是同一函数的别名，见 [remove_spaces](remove_spaces.md)。

## 适用场景

读取 ID、标签或其他需要去除格式空白的文本时，可以先调用这个函数：

```arti
let id = "  steel  "
core.emit(id.remove_space())
```

