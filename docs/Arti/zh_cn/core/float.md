# float

`core.random.float(min, max)` 生成范围内的随机小数：

```arti
let value = core.random.float(0, 1)
let ratio = core.random.float(min: 0.25, max: 0.75)
```

下界包含在范围内，上界按随机浮点数语义近似为不包含。两个端点相等时返回该端点。
