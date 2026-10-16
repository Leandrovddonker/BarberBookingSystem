# BarberBookingSystem

A RESTful backend API for managing barbershop appointments, built with **C# and ASP.NET Core (.NET 8)**.

The application allows customers to book appointments with barbers, view available time slots, and manage reservations. It includes JWT-based authentication, role-based authorization, and automated testing to ensure reliable booking functionality.

> **Project Status: Work in Progress**
>
> This project is still under active development. The core backend functionality is implemented, but additional features, input validation, security improvements, and a frontend application are still planned.

## Tech Stack

- **C# / .NET 8**
- **ASP.NET Core Web API**
- **Entity Framework Core**
- **PostgreSQL**
- **JWT Authentication**
- **BCrypt** – Password hashing
- **xUnit** – Automated testing
- **SQLite In-Memory** – Integration testing
- **Swagger / OpenAPI** – API documentation

## Features

### Authentication & Authorization

- User registration and login
- JWT-based authentication
- Secure password hashing using BCrypt
- Role-based authorization for Admin, Barber, and User
- Protected API endpoints

### Reservation Management

- Create and manage barber appointments
- Select a barber and haircut service
- Retrieve available appointment slots for a selected date
- Prevent overlapping reservations
- Validate appointments against opening hours and service duration
- Cancel reservations

### Barber & Haircut Management

- Manage barber profiles
- Manage haircut services, prices, and durations
- Retrieve available barbers and haircut services

### Availability Calculation

The reservation system dynamically calculates available appointment slots based on:

- Barbershop opening hours
- Selected haircut duration
- Existing reservations
- Selected barber and appointment date

This ensures customers can only book valid time slots without conflicting with existing appointments.

## Project Structure

The solution separates the application from its automated tests.

```text
BarberBookingSystem/
├── BarberBookingSystem/
│   ├── Controllers/
│   ├── Services/
│   ├── Models/
│   ├── DTOs/
│   ├── Data/
│   ├── Program.cs
│   └── appsettings.json
│
├── BarberBookingSystem.Tests/
│
└── BarberBookingSystem.sln
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL
- An IDE such as JetBrains Rider or Visual Studio

### 1. Clone the repository

```bash
git clone https://github.com/Leandrovddonker/BarberBookingSystem.git
cd BarberBookingSystem
```

### 2. Configure application secrets

Sensitive configuration values are managed using **.NET User Secrets** rather than being stored in source control.

From the API project directory, initialize User Secrets if necessary:

```bash
cd BarberBookingSystem
dotnet user-secrets init
```

Configure your PostgreSQL connection string and JWT signing key using the configuration names expected by the application. Never commit real credentials.

### 3. Run the application

From the solution directory:

```bash
dotnet run --project BarberBookingSystem
```

Once running, the API can be accessed through its configured local URL. Swagger can be used to explore and test endpoints when enabled.

## Testing

The project currently includes **24 automated tests**, covering reservation functionality and authentication/authorization behavior.

The test suite includes:

- Reservation service tests
- Appointment availability validation
- Reservation creation and cancellation scenarios
- Controller integration tests
- JWT authentication and authorization checks
- HTTP status validation, including `200`, `401`, and `403`

Integration tests use an in-memory SQLite database to test application behavior without requiring a running PostgreSQL instance.

Run all tests with:

```bash
dotnet test
```

## Security

The application currently implements:

- JWT-based authentication for protected endpoints
- Role-based access control
- BCrypt password hashing
- Separation of sensitive configuration from source code
- Authorization testing for protected routes

Additional security measures and input validation are planned before the application is considered production-ready.

## Roadmap

The following features and improvements are planned for future development.

### Frontend Development

- [ ] Build a user-friendly frontend application
- [ ] Implement registration and login pages
- [ ] Create an appointment booking interface
- [ ] Allow customers to view and manage their reservations
- [ ] Create management interfaces for barbers and administrators

### Validation & Security

- [ ] Implement password strength validation (minimum length, complexity requirements, and rejection of weak passwords such as `password`)
- [ ] Implement email address format validation
- [ ] Add additional security-related tests

### Additional Features

- [ ] Email notifications for appointment confirmations and cancellations
- [ ] Automated CI/CD pipeline using GitHub Actions
- [ ] Docker support for easier deployment
- [ ] Improve API documentation
- [ ] Deploy the application to a cloud environment

## Author

Developed by **Leandro van den Donker** as a backend software development portfolio project.

**GitHub:** [Leandrovddonker](https://github.com/Leandrovddonker)
