# getvar

`getvar(key)` 读取 RimTalk 会话变量：

```arti
let value = getvar("selected_topic")
core.emit(core.value.default(value, "未选择"))
```

不存在的键返回 `null`。会话变量按文本保存，读取结果是文本或 `null`；Pawn、Map 和数组会转换成文本。

最常见的用途是让多个正式 Arti 代码块共享一个简单文本：

```text
{{%
setvar("topic", core.value.default(ctx.topic, "未指定"))
%}}

当前话题：
{{%
core.emit(getvar("topic"))
%}}
```

会话变量属于宿主的 Prompt 会话状态，适合保存短文本。大型内容和临时计算值分别放在提示词正文和局部变量中。
