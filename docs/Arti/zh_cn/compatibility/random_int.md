# random_int

`art.random_int(min, max)` 是旧模板兼容函数，返回包含两端的随机整数：

```text
{{ art.random_int(1, 6) }}
```

它只支持两个参数。新模板建议使用 [`int`](../core/int.md)：

```text
{{%
core.emit(core.random.int(1, 6))
%}}
```
