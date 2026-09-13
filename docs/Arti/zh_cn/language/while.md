# while

`while` 在条件为真时重复执行代码块：

```arti
let count = 0
while count < 3 {
    core.emit(count, newline: true)
    count += 1
}
```

循环条件每次执行前都会重新计算。请确保循环体最终会让条件变为假。

## 控制循环

```arti
let count = 0
while true {
    if count >= 3 {
        break
    }
    count += 1
}
```

`break` 结束当前循环，`continue` 跳过当前迭代。

分析阶段不验证 `while` 的终止条件；实际执行受到运行时步数上限保护，超过上限会报告运行时错误。
