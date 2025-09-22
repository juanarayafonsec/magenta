---
model: auto
temperature: 0.2
---

# Code Generation and Solution Scaffold Task

You are an expert C#/.NET backend engineer.

## Solution and Folder Structure

Create a production-ready C# .NET 9 solution named `Magenta.sln`.

- Place all source projects under `/src` folder:
  - `Magenta.Application` (Application layer)
  - `Magenta.Domain` (Domain layer)
  - `Magenta.Infrastructure` (Infrastructure layer)
  - `Magenta.API` (ASP.NET Core Web API)

- Place all test projects under `/test` folder:
  - `Magenta.Application.Tests`

## Solution and Project Creation Requirements

- Create the `.sln` file `Magenta.sln` and include all projects.
- Create each project with the appropriate project template:
  - Class Library for Application, Domain, Infrastructure
  - ASP.NET Core Web API for API project
- Create folders as needed to match the above paths.
- Configure project references following Clean Architecture.
- Use built-in .NET dependency injection (no third-party DI).
- Avoid third-party libraries except:
  - PostgreSQL EF Core provider
  - Swagger (Swashbuckle) for API documentation

## Registration Feature

- Implement user registration only (sign-up).
- Use ASP.NET Core Identity.
- User model includes only `Username` and `Email`, both unique.
- Enforce uniqueness at both the application and PostgreSQL database level.
- Use Identity's built-in password hasher.
- Provide the registration API endpoint with validation and proper error responses.
- Use manual validation; do not use FluentValidation or MediatR.

## Swagger Requirement

- Integrate Swagger/OpenAPI documentation in the API project.
- Document the registration endpoint fully.

## Additional Guidance

- Follow clean code and Clean Architecture principles.
- Modular code with separation of concerns.
- Provide detailed file headers specifying file paths.
- Include:
  - Domain user entity and interfaces
  - Application logic for registration
  - Infrastructure setup for EF Core with PostgreSQL and Identity
  - API controllers, models, DI, and Swagger configuration
  - Unit tests for registration without third-party frameworks

## Output Instructions

- Output commands or instructions for creating the solution, projects, and folders.
- Output full source code files separated by clear filename headers with relative paths.
- Output any essential configuration files (e.g., `Program.cs`, `appsettings.json`, `.editorconfig`, `.gitignore`).

Output only code and commands, clearly separated.

