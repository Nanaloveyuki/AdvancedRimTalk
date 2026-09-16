# exists

`exists(value)` 检测名称、成员、索引或表达式结果是否存在，返回 `bool`。
支持等价的倒置调用 `value.exists()`，不限制值的类型。

```arti
core.emit(exists(missing))          // false：名称未声明
let value = null
core.emit(value.exists())           // true：已声明，值为 null
core.emit(exists(value: value))     // true：命名参数
let data = { name: "RimWorld", extra: null }
core.emit(data.name.exists())       // true
core.emit(exists(data.extra))       // true
core.emit(data.unknown.exists())    // false
let items = [1, 2]
core.emit(exists(items[5]))          // false：索引不存在
core.emit(exists(false))            // true：false 是存在的值
```

字符串是普通值，`exists("name")` 不会按字符串内容查找变量。
检测存在性不等于检测非空；排除 `null` 时使用 `exists(value) && value != null`。
缺失名称、成员和索引不会触发未声明错误；函数执行、索引表达式或数据提供者自身的错误仍正常报告，不会被隐藏。

```arti
if lang == "简体中文" {
    let language = "必须使用通俗白话"
} else if lang == "繁體中文" {
    let language = "推荐使用通俗白话"
}
if language.exists() {
    core.emit(language)
}
```

函数也可作为普通可调用值传递，例如 `let check = exists`、`check(42)`。
直接检测未声明名称时使用 `exists(name)` 或 `name.exists()`，让静态分析器识别检测上下文。

模块对象原有的 `module.exists()` 仍表示模块是否安装。通用的 `exists(module)` 检测模块绑定本身是否存在；两者用途不同。
