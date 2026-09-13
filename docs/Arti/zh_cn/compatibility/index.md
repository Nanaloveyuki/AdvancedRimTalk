# 兼容

这一组页面说明 Arti 与 RimTalk 既有模板的边界：

- [art](art.md)：旧版 `art.*` 占位符和函数。
- [scriban](scriban.md)：RimTalk 原生 `{{ ... }}`。
- [兼容边界](../guide/mode.md)：嵌入模式与接管模式的行为差异。

正式 Arti 使用 `{{% ... %}}`；兼容层和正式 Arti 是两个入口，处理阶段也各自独立。
