# 复合赋值

`let` 变量支持 `+=`、`-=`、`*=`、`/=` 和 `%=`。运算结果会写回当前作用域的变量：

```arti
let total = 0
for value in [1, 2, 3] {
    total += value
}
core.emit(total)
```

复合赋值要求左侧变量已经声明，`const` 变量不能修改。
