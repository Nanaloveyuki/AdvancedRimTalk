# string

`core.string` 是字符串函数命名空间。字符串函数也注册为全局函数，并支持 [倒置调用](../language/method.md)。

## 调用形式

下面三种形式等价：

```arti
len("Alice")
core.string.len("Alice")
"Alice".len()
```

倒置调用把接收字符串作为函数的 `value` 参数。其他参数仍写在括号中：

```arti
replace("a-b", "-", "_")
core.string.replace("a-b", "-", "_")
"a-b".replace("-", "_")
```

## 函数索引

### 文本长度和空白

- [`len`](string/len.md)：返回文本长度。
- [`remove_space`](string/remove_space.md)：删除所有空白字符。
- [`remove_spaces`](string/remove_spaces.md)：`remove_space` 的别名。
- [`trim`](string/trim.md)：删除两端空白。
- [`trim_start`](string/trim_start.md)：删除开头空白。
- [`trim_end`](string/trim_end.md)：删除结尾空白。
- [`is_empty`](string/is_empty.md)：判断文本是否为空。

### 大小写

- [`upper`](string/upper.md)：转换为大写。
- [`uppercase`](string/uppercase.md)：`upper` 的别名。
- [`lower`](string/lower.md)：转换为小写。
- [`lowercase`](string/lowercase.md)：`lower` 的别名。
- [`capitalize`](string/capitalize.md)：转换首字符大小写。
- [`title`](string/title.md)：转换为标题形式。

### 文本组合和查找

- [`append`](string/append.md)：在文本末尾追加内容。
- [`prepend`](string/prepend.md)：在文本开头追加内容。
- [`replace`](string/replace.md)：替换文本中的所有匹配项。
- [`contains`](string/contains.md)：判断是否包含片段。
- [`starts_with`](string/starts_with.md)：判断是否以片段开头。
- [`ends_with`](string/ends_with.md)：判断是否以片段结尾。

### 文本切分和重复

- [`substring`](string/substring.md)：截取文本。
- [`split`](string/split.md)：按分隔符切分文本。
- [`repeat`](string/repeat.md)：重复文本。

`core.append` 是输出函数；字符串函数 `core.string.append` 用于拼接文本，两者作用不同。
