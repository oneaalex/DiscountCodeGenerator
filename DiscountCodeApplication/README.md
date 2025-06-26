# Discount Code Application

## Overview

This project is a Razor Pages application for generating, managing, and redeeming discount codes. It uses SignalR for real-time communication and provides a simple HTML client for testing.

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

#### Example (using xUnit and Moq):

---

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
