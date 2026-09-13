# def

`core.def` 是通用 Def 选择器：

```arti
let def = core.def("ThingDef", "Silver")
```

也可以使用：

```arti
core.def.all("ThingDef")
core.def.find("ThingDef", "Silver")
core.def.count("ThingDef")
```

类型省略时默认使用 `ThingDef`。查找会匹配 `defName`、显示名称或 `LabelCap`，不区分大小写。

需要某类 Def 的常用快捷入口时，使用 [defs](defs.md)。
