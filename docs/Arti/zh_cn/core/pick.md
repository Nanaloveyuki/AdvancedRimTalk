# pick

`core.random.pick(values)` 从候选值中随机选择一个：

```arti
let mood = core.random.pick("calm", "urgent", "curious")
```

也可以把数组作为唯一参数：

```arti
let mood = core.random.pick(["calm", "urgent", "curious"])
```

没有候选值时返回 `null`。函数只按位置参数处理候选值；需要先整理集合时，可以配合 [`array`](../language/array.md) 和 [`for`](../language/for.md)。
