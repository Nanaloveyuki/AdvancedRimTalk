# 按 Mod 启用提示

用 `core.mod` 检查可选 Mod，存在时追加专用规则；缺少时保持基础提示词：

```arti
if core.mod.loaded("SomeMod.PackageId") {
    core.emit("SomeMod 已启用：遵循其角色设定。", newline: true)
}
```

PackageId 应从 Mod 的 About.xml 或日志确认，避免依赖显示名称。
