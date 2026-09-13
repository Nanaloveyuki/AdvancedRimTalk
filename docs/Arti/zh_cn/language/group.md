# group

`group use` 用于在顶层声明属于同一 Prompt 组的模块：

```arti
group use cj.rimtalk as rimtalk
group use optional some.integration as integration
```

每一行声明一个模块。它是一种组级导入写法，模块访问方式保持不变。

## 位置限制

`group use` 必须位于当前 Arti 程序的顶层。放进 `if`、循环或函数中会产生分析错误：

```arti
if true {
    group use some.mod
}
```

如果模块只在一个分支中使用，改用普通的 [`use`](use.md)。

## 兼容写法

历史上 `const use` 可能被当作分组导入。当前解析器仍接受这种写法并给出兼容性警告，但新文档和新代码应使用 `group use`。
