# Discount Code Application

## Overview

This project is a Razor Pages application for generating, managing, and redeeming discount codes. It uses SignalR for real-time communication and provides a simple HTML client for testing.

---

## Getting Started

To get the Discount Code Application up and running locally in Visual Studio (IIS Express), follow these steps:

### 1. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- Visual Studio 2022 or later
- A running Redis instance (for caching)
- (Optional) SQL Server or your configured database for EF Core

### 2. Configure the Application

- Open the solution in Visual Studio.
- Update your `appsettings.json` with the correct connection strings for your database and Redis server.

### 3. Database Migrations

- If you have not already created the database, open the Package Manager Console in Visual Studio and run:

(Requires the [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet).)

Or use the .NET CLI:

### 4. Run the Application

- Set the project as the startup project in Visual Studio.
- Press **F5** or click the **IIS Express** button to launch the application.
- By default, the app will be available at a URL like `https://localhost:44347` (check the port in your `launchSettings.json`).

### 5. Access the Application

- **Web Application (Razor Pages):** Open your browser and navigate to the IIS Express URL (e.g., `https://localhost:44347`).
- **SignalR Hub:** The SignalR hub is accessible at `/discountCodeHub` (e.g., `https://localhost:44347/discountCodeHub`).

**Important:** The SignalR hub may require a `secret` query parameter for authorization. The default secret is configured in `appsettings.json` under `SignalR:Secret`. You will need to append this to your connection URL, for example:

---

## Accessing the SignalR Hub with testSignalR.html

You can interact with the SignalR hub using the provided HTML client at `Pages/testSignalR.html`. This file demonstrates and allows manual testing of all main hub features.

### How to Use testSignalR.html

1. **Open the File:**  
   Open `Pages/testSignalR.html` in your browser (you may need to serve it via IIS Express or open it directly if CORS is not enforced).

2. **Connect to the Hub:**  
   Click the **Connect** button to establish a connection with the SignalR hub. The connection URL (including the `secret` query parameter) is preconfigured in the script.

3. **Available Actions:**
   - **Generate Codes:**  
     - Set the desired count and length, then click **Generate Codes**.  
     - Invokes the `GenerateCode(count, length)` method on the hub to generate new codes.
   - **Use Code:**  
     - Enter a code in the input box and click **Use Code**.  
     - Invokes the `UseCode(code)` method to attempt to redeem the code.  
     - The result is interpreted and displayed (success, already used, expired, etc.).
   - **Get All Codes:**  
     - Click **Get All Codes** to retrieve and display all available discount codes.  
     - Invokes the `GetCodes()` method.
   - **Get Recent Codes:**  
     - Click **Get Recent Codes** to retrieve and display the most recently generated codes (default: 10).  
     - Invokes the `GetRecentCodes(count)` method.

4. **Messages:**  
   All actions and server responses are displayed in the messages area at the bottom of the page.

### SignalR Hub Methods (Options)

- `GenerateCode(count, length)`  
  Generates a batch of new discount codes. Returns `true` on success.
- `UseCode(code)`  
  Attempts to redeem a code. Returns a result byte:
    - `0`: Success
    - `1`: Not found or invalid
    - `2`: Already used
    - `3`: Expired
    - `4`: Inactive
    - `5`: Deleted
    - `6`: Exception or unknown error
- `GetCodes()`  
  Retrieves all available discount codes as a list of strings.
- `GetRecentCodes(count)`  
  Retrieves the most recent discount codes (up to `count`).

**Note:** The hub also sends server-to-client events such as `CodeUsed`, `CodeGenerated`, and error messages, which are displayed in the UI.

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

#### Running Unit Tests

To run the unit tests, navigate to the `DiscountCodeApplication.Test` directory and execute the following command:
dotnet test

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

**Note:** For best results, ensure your tests cover edge cases such as concurrent usage, expired codes, and repository failures.
