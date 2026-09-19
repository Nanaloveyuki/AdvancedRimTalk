# once

```text
core.once(id, action, note: "")
core.once.done(id)
```

把一段无参函数标记为只执行一次。第一次调用会运行 `action`，成功返回后把记录写入 Mod 配置；之后相同 `id` 在同一作用域内会被跳过。

作用域在已加载的游戏中按世界区分（世界名 + 种子）；主菜单 REPL 使用 `global`。提示词预览既不执行 `action`，也不写记录。

## 示例

```arti
use optional memory as memory

fn seed_lore() {
    if !memory.api_available {
        return
    }
    memory.knowledge.add_ex("指引者", "玩家的化身。", importance: 1.0, match_mode: "Any", can_be_extracted: true, can_be_matched: true)
}

if memory.api_available {
    core.once("colony-lore-v1", seed_lore, note: "注入殖民地常识")
}
```

返回 `true` 表示这次执行了 `action`，`false` 表示已有记录、正在预览，或配置存储不可用时的跳过以外的“未执行”。没有配置存储时（例如单元测试未注入 store）每次都会执行，且不落盘。

`action` 必须是无参可调用值，通常是顶层 `fn`。函数正常返回即视为成功并记一条记录；函数抛出运行时错误时不记记录，下次还会再跑。

不要在 `action` 内部再判断“有没有写过库”作为唯一开关；删除记录后应能重新执行。需要 API 就绪时，在调用 `core.once` 之前检查，避免把失败的空跑记成已完成：

```arti
if memory.api_available && !core.once.done("colony-lore-v1") {
    core.once("colony-lore-v1", seed_lore)
}
```

`note` 可选，会出现在一次性记录页面，用来说明这次做了什么。

## 记录和重新触发

记录保存在 Advanced RimTalk 的 Mod 设置里，可在 IrisMenus 的「一次性执行记录」子页面查看。没有 IrisMenus 时，主设置页也有同名按钮。删除一条记录后，该 `id` 会在下次正式执行时再跑。

预览、查看已捕获提示词都不会新增记录。
