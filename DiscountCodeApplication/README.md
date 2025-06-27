# Discount Code Application

## Overview

This project is a Razor Pages application for generating, managing, and redeeming discount codes. It uses SignalR for real-time communication and provides a simple HTML client for testing.

---

## Recent Changes

### 1. Discount Code Cache Preloading

- **Background Preloading:**  
  The application now preloads discount code caches in the background at startup using a hosted service (`DiscountCodePreloadHostedService`). This ensures that recent and all discount codes are available in the cache without blocking the main application thread.
- **Dependency Injection:**  
  The repository responsible for preloading is injected into the hosted service, following best practices for maintainability and testability.

### 2. SignalR Package

- The unnecessary `Microsoft.AspNetCore.SignalR` package reference was removed from the project dependencies.

### 3. Authorization and User Management

> **Note:**  
> The project was designed to use JWT (JSON Web Token) for authorization. However, implementing JWT-based authentication requires a user management system, including user registration, login, and logout functionality.  
>  
> These features (user accounts, login, logout) are not currently implemented in this project. To enable secure JWT authorization, you would need to add:
> - User registration and storage (e.g., with ASP.NET Core Identity or a custom user table)
> - Login endpoints to issue JWT tokens upon successful authentication
> - Logout mechanism (typically handled client-side by removing the JWT)
>  
> Without these, JWT authorization cannot be fully enabled.

---

## How the DiscountCodeService Works

The `DiscountCodeService` is responsible for generating, storing, retrieving, and using discount codes in a thread-safe manner. It interacts with a repository for data access, a code generator for creating unique codes, and a unit of work for transaction management.

### Main Methods

- **GenerateAndAddCodesAsync(ushort count, byte length):**
  - Generates a specified number of unique discount codes of a given length.
  - Ensures no duplicate codes are created.
  - Saves the new codes to the database in a single transaction.

- **GetAllCodesAsync():**
  - Retrieves all discount codes from the database.

- **GetMostRecentCodesAsync(int count = 10):**
  - Retrieves the most recently created discount codes, defaulting to 10.

- **UseCodeAsync(string code):**
  - Marks a discount code as used if it is valid, active, not expired, and not already used.
  - Handles concurrency using per-code locks to prevent race conditions.

### Thread Safety

- Uses a global semaphore to prevent concurrent code generation.
- Uses a per-code semaphore to ensure that each code can only be used by one process at a time.

---

## How to Test

You can test the service using unit tests or integration tests. Here are some suggested approaches:

### 1. Unit Testing

- **Mock** the `IDiscountCodeRepository`, `IDiscountCodeGenerator`, and `IUnitOfWork` dependencies.
- Test each method for expected behavior, including:
  - Generating the correct number of codes.
  - Preventing duplicate codes.
  - Correctly marking codes as used.
  - Handling invalid, expired, or already-used codes.
  - Ensuring thread safety by simulating concurrent calls.

### 2. Integration Testing

- Set up a test database.
- Use the real implementations of the repository and unit of work.
- Test the full workflow: generate codes, retrieve them, and use them.

### 3. Manual Testing

- Use the Razor Pages UI (if available) to generate and use codes.
- Check logs for expected informational and error messages.

---

## Testing Discount Codes with `testSignalR.html`

The `Pages/testSignalR.html` file provides a simple web interface to interact with the SignalR hub for discount code operations. This allows you to manually test the main features of the system in real time.

### Features

- **Connect:** Establishes a connection to the SignalR hub.
- **Generate Codes:** Requests the server to generate a batch of new discount codes.
- **Get All Codes:** Retrieves and displays all available discount codes.
- **Get Recent Codes:** Retrieves and displays the most recently generated codes.
- **Use Code:** Attempts to use a specific discount code entered in the input box.

### How to Use

1. **Open the File:**  
   Open `Pages/testSignalR.html` in your browser.

2. **Connect to the Hub:**  
   Click the **Connect** button to establish a connection with the SignalR hub.

3. **Generate Codes:**  
   Click **Generate Codes** to create a set of new discount codes (default: 5 codes of length 8).

4. **Get All Codes:**  
   Click **Get All Codes** to display all codes currently stored.

5. **Get Recent Codes:**  
   Click **Get Recent Codes** to display the latest codes (default: 10).

6. **Use a Code:**  
   Enter a code in the input box and click **Use Code** to attempt to redeem it. The result will be shown in the messages area.

### Notes

- All actions and server responses are displayed in the messages area at the bottom of the page.
- Make sure your backend and SignalR hub are running and accessible at the URL specified in the script (`https://localhost:44347/discountCodeHub` by default).
- You can modify the count and length parameters in the script if you want to generate a different number or length of codes.

---

## How to Run the Application

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- A running Redis instance (for caching)
- (Optional) SQL Server or your configured database for EF Core

### 1. Configure the Application

- Update your `appsettings.json` with the correct connection strings for your database and Redis server.
- Example:

- By default, the app will be available at `https://localhost:44347` (or the port specified in `launchSettings.json`).

### 2. Build and Run

- Open a terminal in the project root.
- Run the following commands:

### 3. Database Migrations

- If you have not already created the database, run:

(Requires the [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet).)

Follow the instructions in that branch's README or Docker-related files to build and run the application in a containerized environment.

---

## Running the Application

By default, the application runs in your local development environment.

### Running with Docker

If you want to run the app in Docker, switch to the `feature_docker` branch:

---

## How to Use the Application

### Razor Pages UI

- Navigate to `https://localhost:44347` in your browser.
- Use the provided Razor Pages interface to:
  - Generate new discount codes
  - View all codes
  - Redeem codes

### Real-Time Testing with SignalR

- Open `Pages/testSignalR.html` in your browser.
- Use the buttons to:
  - Connect to the SignalR hub
  - Generate codes
  - Retrieve all or recent codes
  - Redeem a code

All actions and responses will be shown in the messages area at the bottom of the page.

### API Endpoints (if exposed)

- The application may expose API endpoints for discount code operations. Check the `Hubs/DiscountCodeHub.cs` and Razor Pages handlers for available endpoints and their usage.

---

## Notes

- Ensure Redis and your database are running before starting the app.
- Logs are written using Serilog; check the console or configured sinks for details.
- For development, you may need to trust the ASP.NET Core development certificate.

---

**You are now ready to run and use the Discount Code Application!**
