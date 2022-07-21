namespace RegexBot;

/// <summary>
/// Provides a hint to the module loader that the class it is applied to should be treated as a module instance.
/// When the program scans an assembly, it is scanned for classes which implement <see cref="RegexbotModule"/> and have this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegexbotModuleAttribute : Attribute { }
