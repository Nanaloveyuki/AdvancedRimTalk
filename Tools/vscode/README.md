# Arti Language Support for VS Code

这是 Advanced RimTalk Arti DSL 的静态 TextMate 高亮扩展，当前放在
`Tools/vscode` 供项目内开发和分发。

## 支持范围

- `.arti` 文件自动使用 `Arti` 语言模式。
- Markdown 的 ```` ```arti ```` / `~~~arti` 代码围栏使用 Arti 高亮。
- Markdown 中正式的 `{{% ... %}}` Arti 代码块使用 Arti 高亮。
- 关键字、布尔/null 字面量、字符串和转义、数字、注释、函数声明/调用、
  模块导入、内置根名称、成员访问、操作符和括号分别提供 TextMate scopes。
- Markdown 的 fenced code、缩进代码块和行内代码中的示例不会被 Arti 块注入规则误识别。

## 明确不包含

扩展没有 `main`、`extension.ts` 或其他运行时代码，因此不执行 Arti，也不提供：

- 动态语法解析或语义分析
- 诊断、补全、跳转、重命名或符号索引
- 格式化、代码执行或运行时模块/变量发现

高亮规则是根据 `docs/Arti/zh_cn` 和 `Source/Arti/ArtiLexer.cs` 中当前正式词法手工维护的。

## 本地调试

在仓库根目录执行：

```powershell
code --extensionDevelopmentPath=F:\repo\AdvancedRimTalk\Tools\vscode
```

也可以在 VS Code 中直接打开 `.arti` 文件，或打开 `docs/Arti/zh_cn` 下的
Markdown 文档查看 fenced code 和 `{{% ... %}}` 代码块效果。

如需生成 `.vsix`，运行可复用的打包脚本。默认输出到仓库根目录的
`vscode` 文件夹，并且会覆盖同名的生成包：

```powershell
.\Tools\vscode\scripts\package.ps1
```

也可以从扩展目录通过 npm script 执行：

```powershell
Push-Location Tools/vscode
npm run package:vsix
Pop-Location
```

脚本优先使用 PATH 中已有的 `vsce`，否则通过 `npx --yes @vscode/vsce`
临时调用官方打包工具。可以用 `-OutputDirectory` 指定其他输出目录，或用
`-NoOverwrite` 禁止覆盖同名包：

```powershell
.\Tools\vscode\scripts\package.ps1 -OutputDirectory .\vscode -NoOverwrite
```
