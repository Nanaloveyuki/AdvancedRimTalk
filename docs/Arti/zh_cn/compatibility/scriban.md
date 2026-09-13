# scriban

RimTalk 原生 Scriban 使用 `{{ ... }}`，与 Arti 的 `{{% ... %}}` 是两套语法：

```text
{{ pawn.name }}           // Scriban
{{% core.emit(pawn.name) %}}   // Arti
```

Arti 和 Scriban 各自保留原有语法。嵌入模式中，Arti 先执行，旧 `art.*` 兼容层随后展开，剩余的普通 `{{ ... }}` 继续交给 RimTalk。

## 什么时候使用哪一种

- 维护已有 RimTalk 预设：继续使用原生 Scriban。
- 需要明确的变量作用域、函数、循环和输出边界：使用 Arti。
- 维护旧模板兼容：继续使用 `art.*`；新功能优先写正式 Arti。

普通 Scriban 的函数、管道和控制流属于另一套语法。`{{% ... %}}` 中使用 Arti 语法。
