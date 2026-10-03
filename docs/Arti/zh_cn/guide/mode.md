# mode

Advanced RimTalk 有两种运行方式。设置项中的名称可能会随界面语言变化，但含义如下。

## 嵌入模式

嵌入模式是默认方式：

1. RimTalk 按原有规则收集提示词条目。
2. Advanced RimTalk 先执行条目正文中的 `{{% ... %}}`。
3. 旧版 `art.*` 占位符继续展开。
4. RimTalk 原生 Scriban 继续渲染。
5. 最终消息仍按 RimTalk 原有角色和顺序发送。

这个模式适合只想在现有预设中加入少量动态内容的场景。它保留：

- system/user/assistant 角色；
- 提示词条目的相对位置和 in-chat 位置；
- 对话历史；
- 其他 Mod 注册的提示词条目和上下文变量；
- RimTalk 原有的模型请求流程。

示例：

```text
请回答下面的问题。

{{%
if ctx.is_monologue {
    core.emit("这是独白，请使用第一人称。", newline: true)
}
%}}

{{ art.prompt }}
```

## 接管模式

接管模式拦截 RimTalk 的 `PromptManager.BuildMessages`，按启用的 Prompt Parts 构建消息：

- 每个 Part 自行指定角色、顺序和正文；
- 先执行正式 Arti 块，再由 RimTalk Scriban 渲染原模板中的 `{{ ... }}`；
- Arti 输出中的花括号按普通文本保留，不会再次执行；
- 连续的相同角色会合并，消息受总字符预算限制；
- RimTalk 仍负责触发、参与者选择、模型客户端和回复处理。

接管模式跳过 RimTalk 当前预设的组装，不替换 Scriban。默认 Parts 提供系统指令、JSONL 输出约束、上下文、历史和原始请求；不会自动补上 RimTalk 原有的请求装饰。模板需要自行提供所需内容。

接管生成期间保留 RimTalk 的记忆和紧凑历史开关。它会填写因果请求摘要，供 RimTalk 保存后续对话历史。上下文数量和历史数量设置不能恢复 RimTalk 在接管前已经裁掉的参与者或历史。

## 如何选择

| 场景 | 建议 |
| --- | --- |
| 只想给现有预设增加条件、循环或游戏数据 | 嵌入模式 |
| 需要完全控制 system/user 消息内容 | 接管模式 |
| 依赖其他 Mod 注入的提示词条目 | 先使用嵌入模式 |
| 正在迁移旧的 RimTalk 预设 | 先使用嵌入模式，确认结果后再考虑接管 |

## 常见误解

### 接管模式只替换 Prompt 组装

它替换提示词消息的组装位置；RimTalk 继续负责对话触发、参与者、模型请求和响应解析。

### 嵌入模式仍需显式输出

表达式只负责计算，输出由 [`emit`](../core/emit.md) 或 [`emit_if`](../core/emit_if.md) 完成。

### 两种模式使用的文档不同

嵌入模式执行 RimTalk 提示词条目中的 Arti 块；接管模式执行独立保存的 Prompt Parts。可以导入 RimTalk 预设，源码和位置元数据会保留，但接管模式不保证与原预设的历史装配效果一致。
