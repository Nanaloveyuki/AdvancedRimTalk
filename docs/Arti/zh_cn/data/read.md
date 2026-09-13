# read

`core.read(target, member)` 读取一个显式命名空间成员，或读取对象的公开实例字段/属性：

```arti
let value = core.read(pawn, "name")
let current_map = core.read(core.find, "CurrentMap")
```

读取不到时返回 `null`。静态 `Verse.Find` 成员应通过 `core.find.read` 或 `core.find.get` 读取。

这个函数只提供公开数据读取，访问范围限于公开字段和属性。私有字段、方法、文件和网络都不属于读取接口。

成员名匹配会兼容大小写、下划线、连字符和空格的差异。例如，`unique_id`、`uniqueId` 和 `Unique-ID` 可以指向同一个公开成员。Arti 本身的变量名仍遵循 [identifier](../language/identifier.md) 的规则。
