# object

使用花括号和冒号创建对象：

```arti
let request = {
    kind: "question",
    urgent: true,
    count: 2,
}
```

对象键可以是标识符或字符串：

```arti
let a = { name: "Alice" }
let b = { "display-name": "Alice" }
```

带连字符的键使用索引读取：

```arti
let value = b["display-name"]
```

普通标识符键可以使用成员访问：

```arti
let name = a.name
```

对象主要用于整理中间值，或把结构化数据交给当前支持的读取函数。需要 JSON 字符串时，先组织字段，再使用 [`json`](../core/json.md) 转义文本值。
