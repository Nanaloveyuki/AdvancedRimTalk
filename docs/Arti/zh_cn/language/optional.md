# optional

`optional` 是 [`use`](use.md) 的模块修饰符，表示某个集成不是运行脚本的必要条件：

```arti
use optional "some.mod-with-dash" as some_mod

if some_mod.installed && some_mod.active {
    core.emit("可选 Mod 已准备好")
}
```

### `use optional` 的行为

- 模块未安装时，导入检查直接通过。
- 模块已安装但未启用时，状态会反映为未激活。
- 模块没有可用 API 时，先检查状态，再访问它的专属变量或函数。
- 别名的状态属性只读，模块状态由游戏设置控制。

`optional` 只处理模块依赖；其他未知名称仍按普通名称解析。
