# settings

Advanced RimTalk 的设置决定 Arti 如何接入 RimTalk。

## 嵌入/接管

默认是嵌入模式。启用“接管 RimTalk Prompt 机制”后，Advanced RimTalk 使用接管文档组装 system/user 消息，见 [mode](mode.md)。

## 旧占位符兼容层

默认启用旧 `art.*` 占位符兼容层。它主要用于嵌入模式中的旧 RimTalk 预设。

如果关闭它，`{{% ... %}}` 正式 Arti 仍然可以使用，但 `art.*` 不再由 Advanced RimTalk 展开。

## 接管文档

选择接管模式时，可以编辑接管 Arti 文档。它是实际的 system 内容来源：

- 文档中的 `{{% ... %}}` 会执行；
- 其他文字原样保留；
- 当前启用的 RimTalk 预设属于嵌入路径；接管文档需要自己提供完整内容。

修改后建议先用一个简单的 `core.emit("test")` 验证接管路径，再逐步加入上下文和查询。
