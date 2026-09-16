# 字面量

Arti 支持布尔值、空值、整数、小数、字符串、数组和对象字面量：

```arti
let enabled = true
let missing = null
let count = 3
let ratio = 0.5
let tags = ["calm", "short"]
let profile = { name: "Alice", active: enabled }
```

`null` 可与 `??` 一起提供后备值：

```arti
let name = pawn.nickname ?? pawn.name ?? "未知角色"
```
