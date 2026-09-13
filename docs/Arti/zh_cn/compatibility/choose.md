# choose

`art.choose(value, ...)` 从一个或多个参数中随机选择一个：

```text
{{ art.choose("calm", "urgent", "curious") }}
```

至少需要一个参数。新代码可以使用 [`pick`](../core/pick.md)：

```text
{{%
core.emit(core.random.pick("calm", "urgent", "curious"))
%}}
```
