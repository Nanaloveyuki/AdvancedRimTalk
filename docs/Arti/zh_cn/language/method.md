# method

Arti 为字符串函数提供倒置调用语法：把字符串放在函数名左侧，字符串作为函数的第一个参数。

## 基本写法

下面三种写法得到相同结果：

```arti
let text = " RimWorld "

let a = len(text)
let b = core.string.len(text)
let c = text.len()

core.emit(a + "|" + b + "|" + c)
```

`len(text)`、`core.string.len(text)` 和 `text.len()` 都返回 `10`。

倒置调用的结构是：

```arti
receiver.function_name(other_arguments...)
```

`receiver` 必须是字符串。函数需要的其他参数写在括号中：

```arti
let result = "a-b".replace("-", "_")
core.emit(result)
```

## 命名参数

倒置调用保留原函数的命名参数。接收字符串对应参数 `value`，其他参数继续按原名称传入：

```arti
let result = "a".prepend(prefix: "b")
let part = "abcdef".substring(start: 1, length: 3)

core.emit(result + "|" + part)
```

这段代码输出 `ba|bcd`。

## 链式调用

返回文本的字符串函数可以把结果交给下一步；长度和判断函数返回数字或布尔值：

```arti
let result = "  hello rimworld  ".trim().upper().replace("RIMWORLD", "RimWorld")

core.emit(result)
```

链式调用适合连续整理文本。步骤较多时，可以拆成多个局部变量。

## 可用函数

倒置调用使用 `core.string` 提供的字符串函数。全局函数名和 `core.string` 命名空间都保留：

| 函数 | 普通调用 | 倒置调用 |
| --- | --- | --- |
| [`len`](../core/string/len.md) | `len(text)` | `text.len()` |
| [`remove_space`](../core/string/remove_space.md) | `remove_space(text)` | `text.remove_space()` |
| [`append`](../core/string/append.md) | `append(text, suffix)` | `text.append(suffix)` |
| [`prepend`](../core/string/prepend.md) | `prepend(text, prefix)` | `text.prepend(prefix)` |
| [`upper`](../core/string/upper.md) | `upper(text)` | `text.upper()` |
| [`lower`](../core/string/lower.md) | `lower(text)` | `text.lower()` |
| [`capitalize`](../core/string/capitalize.md) | `capitalize(text)` | `text.capitalize()` |
| [`title`](../core/string/title.md) | `title(text)` | `text.title()` |
| [`trim`](../core/string/trim.md) | `trim(text)` | `text.trim()` |
| [`trim_start`](../core/string/trim_start.md) | `trim_start(text)` | `text.trim_start()` |
| [`trim_end`](../core/string/trim_end.md) | `trim_end(text)` | `text.trim_end()` |
| [`replace`](../core/string/replace.md) | `replace(text, old, replacement)` | `text.replace(old, replacement)` |
| [`contains`](../core/string/contains.md) | `contains(text, part)` | `text.contains(part)` |
| [`starts_with`](../core/string/starts_with.md) | `starts_with(text, part)` | `text.starts_with(part)` |
| [`ends_with`](../core/string/ends_with.md) | `ends_with(text, part)` | `text.ends_with(part)` |
| [`substring`](../core/string/substring.md) | `substring(text, start, length?)` | `text.substring(start, length?)` |
| [`split`](../core/string/split.md) | `split(text, separator)` | `text.split(separator)` |
| [`repeat`](../core/string/repeat.md) | `repeat(text, count)` | `text.repeat(count)` |
| [`is_empty`](../core/string/is_empty.md) | `is_empty(text)` | `text.is_empty()` |

别名 [`remove_spaces`](../core/string/remove_spaces.md)、[`uppercase`](../core/string/uppercase.md) 和 [`lowercase`](../core/string/lowercase.md) 也支持两种调用形式。

## 自定义函数

`fn` 声明的自定义函数使用普通调用：

```arti
fn add_prefix(value) {
    return "角色：" + value
}

core.emit(add_prefix("Alice"))
```

倒置调用专用于列出的字符串函数。
