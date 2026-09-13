# len

`len(value)` 返回文本长度。输入先转换为文本，`null` 按空文本处理。

## 语法

```arti
let count = len("RimWorld")
let count2 = core.string.len("RimWorld")
let count3 = "RimWorld".len()
```

三种写法都会返回 `8`。倒置调用见 [method](../../language/method.md)。

## 示例

```arti
let name = "未知角色"
if pawn != null {
    name = core.value.default(pawn.name, "未知角色")
}
if name.len() > 8 {
    core.emit("角色名称较长")
}
```
