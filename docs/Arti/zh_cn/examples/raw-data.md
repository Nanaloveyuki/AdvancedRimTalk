# 读取并拆解原始数据

读取其他 Mod 提供的键值数据后，按步骤检查字段，再组合成稳定文本：

```arti
let raw = core.read("SomeMod", "state")
let lines = core.string.split(core.value.default(raw, ""), "\\n")
for line in lines {
    let clean = core.string.trim(line)
    if !core.string.is_empty(clean) {
        core.emit("- " + clean, newline: true)
    }
}
```

先输出字段名确认格式，再增加筛选条件；这样 Mod 更新后更容易定位变化。
