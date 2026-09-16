# use

`use` 把模块绑定到当前作用域：

```arti
use some.mod as some_mod

if some_mod.active {
    core.emit("模块已启用")
}
```

模块绑定是只读的。`use` 负责准备访问入口，模块状态和游戏数据仍由宿主提供。

## 别名

推荐总是显式写 `as`：

```arti
use "cj.rimtalk" as rimtalk
use optional "Some.Mod-With-Dash" as optional_mod
```

如果不写别名，Arti 会使用包 ID 的最后一段作为别名。例如 `foo.bar` 默认使用 `bar`。包 ID 含连字符时，应使用引号并显式指定合法别名。

## 必需模块和可选模块

普通 `use` 表示模块是必需的：

```arti
use cj.rimtalk as rimtalk
```

如果模块没有安装、没有启用或没有可用 API，分析阶段会报告错误。

`use optional` 表示模块可以不存在：

```arti
use optional some.integration as integration

if integration.active {
    core.emit("可选集成已启用")
}
```

可选模块不存在时，别名仍然可以使用，状态显示为未安装；访问额外数据前先检查模块状态。

## 作用域

普通 `use` 可以出现在代码块或函数中。控制流花括号不隔离别名；函数中的别名只在该函数及其内部可见。

模块别名必须避开保留的内置根名称，例如 `core`、`ctx`、`pawn`、`map`、`query`、`random`、`setvar` 和 `getvar`。

## `use core`

`core` 始终是内置根模块，通常直接访问即可。下面两种写法都可以访问 `core`：

```arti
core.emit("hello")

use core
core.emit("hello")
```

## 相关页面

- [group](group.md)：在顶层批量声明模块。
- [optional](optional.md)：只看可选模块写法。
- [mod](../core/mod.md)：读取模块状态。
