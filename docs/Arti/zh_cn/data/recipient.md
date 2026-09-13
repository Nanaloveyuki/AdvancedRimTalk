# recipient

`recipient` 是当前对话的接收 Pawn。它使用与 [`pawn`](pawn.md) 相同的成员读取规则：

```arti
if recipient != null {
    core.emit("对话对象：" + recipient.name)
}
```

常用成员包括 `name`、`faction`、`id`、`map`、`position`、`mood`、`health`、`traits`、`skills`、`relations` 和 `context`。

接收者可能为空。不要无条件读取 `recipient.name`；需要默认值时使用：

```arti
let name = "没有接收者"
if recipient != null {
    name = core.value.default(recipient.name, "没有接收者")
}
```
