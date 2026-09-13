# packageid

`core.packageid(id)` 是 [`mod`](mod.md) 的兼容别名：

```arti
let mod = core.packageid("cj.rimtalk")
core.emit(mod.package_id)
```

它与 `core.mod(id)` 返回相同的模块状态对象。新代码可以直接使用 `core.mod`；保留 `packageid` 是为了兼容旧模板中的命名。
