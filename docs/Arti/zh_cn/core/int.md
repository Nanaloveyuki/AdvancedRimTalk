# int

`core.random.int(min, max)` 生成一个包含 `min` 和 `max` 的随机整数：

```arti
let roll = core.random.int(1, 6)
```

也支持命名参数：

```arti
let roll = core.random.int(min: 1, max: 6)
```

`min` 大于 `max` 时会失败。随机值来自当前 Prompt 执行上下文，适合生成本次提示词内容；稳定 ID 应使用游戏对象自身的 ID。
