# comment

Arti 当前支持 `//` 行注释：

```arti
// 这是注释
let name = pawn.name // 行尾也可以写注释
```

注释从 `//` 开始，一直到当前行结束。字符串中的 `//` 属于普通字符：

```arti
let url = "https://example.invalid"
```

块注释 `/* ... */` 会产生词法错误；多行注释请逐行使用 `//`。
