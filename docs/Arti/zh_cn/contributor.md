# Mod Contributor 接入

AdvancedRimTalk 提供通用 raw 读取，因此 Mod 不需要依赖本项目即可被发现。需要向 prompt 提供稳定、语义化数据时，可以实现可选 Contributor。

## 接口约定

Contributor 程序集引用 `AdvancedRimTalk.dll`，实现：

```csharp
public interface IArtiModDataContributor
{
    string PackageId { get; }
    void Register(IArtiModDataRegistry registry);
}
```

注册表提供只读数据工厂：

```csharp
registry.Register("strongholds", context => new Dictionary<string, object>
{
    ["active"] = IsActive(),
    ["count"] = Count(),
});
```

`PackageId` 必须与 Mod 的 `About.xml` 一致。工厂在 Arti 模板实际访问时才执行；不要在注册阶段扫描地图或执行有副作用的操作。

## Arti 中使用

```arti
if core.mods.has("your.package.id")
  let state = core.mods.raw("your.package.id")
  emit json.format(state)
end
```

Contributor 数据应通过 Mod 的命名空间暴露，并保持字段稳定、值可序列化。用户仍可使用 `core.mods.raw(id)` 读取未注册的公共字段和属性。

## 限制

- 只提供只读状态，不注册写入或任意方法调用。
- 避免返回循环引用、Unity 原生对象、线程、文件句柄和网络连接。
- 控制列表规模和文本长度；prompt 总字符预算由 AdvancedRimTalk 统一限制。
- 缺少目标 Mod 时应返回空数据或跳过，不要抛出异常。

完整接口以随 Mod 部署的 `Documentation` 文件为准。
