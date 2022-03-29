namespace RegexBot;

/// <summary>
/// Represents errors that occur when a module attempts to create a new guild state object.
/// </summary>
public class ModuleLoadException : Exception {
    /// <summary>
    /// Initializes this exception class with the specified error message.
    /// </summary>
    public ModuleLoadException(string message) : base(message) { }
}
