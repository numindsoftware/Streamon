namespace Streamon;

[Serializable]
public class InvalidStreamStateException(string message = "Invalid Stream state") : Exception(message) { }
