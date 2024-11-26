namespace Streamon;

[Serializable]
public class InvalidStreamStateException(string message = "Invalid Stream state", Exception? innerException = default) : Exception(message, innerException) { }
