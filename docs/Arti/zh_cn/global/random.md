# random

Arti 有两种随机数入口：

## 全局兼容函数

`random(min, max)` 返回一个包含两端的随机整数：

```arti
let roll = random(1, 6)
```

它使用当前 Prompt 的随机数源。这个入口主要用于兼容简单模板；新代码可以使用 [`int`](../core/int.md)：

```arti
let roll = core.random.int(1, 6)
```

## `core.random`

`core.random` 提供整数、小数和候选值选择：

- [`int`](../core/int.md)：包含两端的整数；
- [`float`](../core/float.md)：范围内的小数；
- [`pick`](../core/pick.md)：从候选值中选择一个。

随机值在本次 Prompt 生成时计算，每次生成都可能变化。稳定 ID 应使用游戏对象自身的 ID。
