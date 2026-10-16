namespace BarberBookingSystem.Exceptions;

public class ForbiddenException(string message) : Exception(message);