namespace RegexBot;

/// <summary>
/// Specifies to the Kerobot module loader that the target class should be treated as a module instance.
/// When the program scans an assembly which has been specified in its instance configuration to be loaded,
/// the program searches for classes implementing <see cref="RegexbotModule"/> that also contain this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegexbotModuleAttribute : Attribute { }
