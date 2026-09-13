# title

`title(value)` 把文本转换为标题形式。函数先将文本转为小写，再按不变区域性规则处理单词首字母。

## 语法

```arti
let text = title("hello RIMWORLD")
let text2 = core.string.title("hello RIMWORLD")
let text3 = "hello RIMWORLD".title()
core.emit(text)
```

结果是 `Hello Rimworld`。

