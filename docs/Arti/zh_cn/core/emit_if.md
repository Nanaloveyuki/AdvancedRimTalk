# emit_if

`core.emit_if(condition, text, newline: false)` 只在条件为真时输出文本，并返回 `text`：

```arti
core.emit_if(pawn != null, "有当前角色", newline: true)
```

条件为假时不写入任何文本，但函数仍返回传入的 `text` 值。

它适合很短的条件输出：

```arti
core.emit_if(ctx.is_monologue, "请使用第一人称。\n")
```

需要多个语句或多个分支时，使用 [`if`](../language/if.md) 更易读。
