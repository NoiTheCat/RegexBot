namespace RegexBot;

/// <summary>
/// Represents an error occurring when a module attempts to create a new guild state object
/// (that is, read or refresh its configuration).
/// </summary>
public class ModuleLoadException : Exception {
    /// <summary>
    /// Initializes this exception class with the specified error message.
    /// </summary>
    public ModuleLoadException(string message) : base(message) { }
}
