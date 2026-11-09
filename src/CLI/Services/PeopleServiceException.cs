namespace CLI.Services;

internal sealed class PeopleServiceException(string message, Exception? innerException = null) : Exception(message, innerException);
