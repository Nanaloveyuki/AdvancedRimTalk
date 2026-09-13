# random

`core.random` 是随机值命名空间：

| 成员 | 用途 |
| --- | --- |
| [`int`](int.md) | 生成包含两端的随机整数 |
| [`float`](float.md) | 生成范围内的随机小数 |
| [`pick`](pick.md) | 从候选值中随机选择 |

```arti
let number = core.random.int(1, 6)
let mood = core.random.pick(["calm", "urgent"])
```

如果只需要兼容旧模板的全局随机整数入口，可以看 [global/random](../global/random.md)。
