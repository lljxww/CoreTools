using System.Reflection;

namespace CoreTools.ApiCaller.Models;

public static class CallerOption
{
    internal static readonly Dictionary<string, Func<CallerContext, CallerContext>> AuthorizeFuncs = [];

    public static void RegisterAuthorizers(Type callerType)
        => RegisterAuthorizers(callerType?.Namespace);

    /// <summary>
    /// 自动扫描并注册所有 ICallerAuthorize 实现类
    /// </summary>
    public static void RegisterAuthorizers(string? namespaceStr)
    {
        var authorizeTypes = GetAuthorizeTypes(namespaceStr);

        foreach (var type in authorizeTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is not ICallerAuthorize instance)
                {
                    continue;
                }

                var name = instance.GetAuthorizeName();

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.Error.WriteLine($"[Authorize] 类型 {type.FullName} 返回了空的授权名称，已跳过。");
                    continue;
                }

                if (AuthorizeFuncs.ContainsKey(name))
                {
                    Console.Error.WriteLine($"[Authorize] 重复的授权名称: {name} 来自 {type.FullName}，已忽略。");
                    continue;
                }

                AuthorizeFuncs[name] = instance.Func;
                Console.WriteLine($"[Authorize] 已注册: {name} -> {type.FullName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Authorize] 注册 {type.FullName} 时出错: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 扫描指定命名空间下所有实现 ICallerAuthorize 的类型。
    /// </summary>
    private static List<Type> GetAuthorizeTypes(string? targetNamespace)
    {
        var baseType = typeof(ICallerAuthorize);

        // 建议缓存，防止频繁扫描
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a =>
                !a.IsDynamic &&
                a.FullName is not null &&
                !a.FullName.StartsWith("System", StringComparison.Ordinal) &&
                !a.FullName.StartsWith("Microsoft", StringComparison.Ordinal) &&
                !a.FullName.StartsWith("netstandard", StringComparison.Ordinal))
            .ToList();

        var result = new List<Type>(capacity: 64);

        foreach (var asm in assemblies)
        {
            foreach (var type in SafeGetTypes(asm))
            {
                if (type is null ||
                    type.IsInterface ||
                    type.IsAbstract ||
                    !baseType.IsAssignableFrom(type))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(targetNamespace) ||
                    (type.Namespace?.StartsWith(targetNamespace, StringComparison.Ordinal) ?? false))
                {
                    result.Add(type);
                }
            }
        }

        return result;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var missing = ex.LoaderExceptions
                .OfType<FileNotFoundException>()
                .Select(e => e.FileName)
                .Distinct();

            Console.Error.WriteLine($"[Authorize] {assembly.GetName().Name} 类型加载不完整: {string.Join(", ", missing)}");

            return ex.Types.Where(t => t != null)!;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Authorize] 无法加载程序集 {assembly.GetName().Name}: {ex.Message}");
            return [];
        }
    }
}
