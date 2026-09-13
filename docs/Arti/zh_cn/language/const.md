# const

`const` 声明一个不可重新赋值的局部变量：

```arti
const prefix = "角色："
const name = pawn.name
```

下面的写法会产生错误：

```arti
const value = 1
value = 2
```

`const` 只限制变量重新赋值。跨生成缓存由宿主的其他机制决定；对象内部的可变性也不由这个关键字控制。Arti 对宿主游戏数据仍然只读。

如果值需要在同一作用域中更新，使用 [`let`](let.md)。
