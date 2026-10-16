namespace BarberBookingSystem.Exceptions;

public class BadRequestException(string message) : Exception(message);