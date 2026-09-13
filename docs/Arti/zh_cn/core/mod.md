# mod

`core.mod(id)` 返回一个表示 Mod 状态的只读对象：

```arti
let rimtalk = core.mod("cj.rimtalk")

if rimtalk.active {
    core.emit("RimTalk 正在运行")
}
```

`id` 可以是包 ID、Mod 文件夹名或 Mod 名称。找不到时返回一个未安装状态。

## 状态成员

| 成员 | 含义 |
| --- | --- |
| `package_id` / `packageId` | 包 ID |
| `installed` | 是否已安装 |
| `active` | 是否已启用 |
| `api_available` / `apiAvailable` | 当前集成 API 是否可用 |
| `version` | 版本文本 |
| `exist` / `exists` | 可调用的安装状态检查 |

示例：

```arti
let mod = core.mod("Some.Mod")
if mod.installed && mod.active && mod.api_available {
    core.emit("可以使用该集成")
}
```

模块状态是读取结果；启用、禁用和加载 Mod 由游戏设置控制。
