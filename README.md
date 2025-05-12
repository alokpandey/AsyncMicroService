# Asynchronous API with Kafka and Webhooks

This project demonstrates an asynchronous API implementation using Kafka for message processing and webhooks for notifications. It consists of two main components:

1. **InventoryService**: A .NET Core Web API that simulates long-running inventory processing tasks
2. **SimpleClient**: A .NET Core console application that interacts with the service and receives webhook callbacks

## System Architecture

The system follows this workflow:
1. Client registers a webhook with the server
2. Client calls the `processInventory` endpoint
3. Server returns a 202 Accepted response with a job ID
4. Server publishes the job to Kafka
5. Server processes the job asynchronously
6. When the job completes, server notifies the client via the registered webhook

## Prerequisites

- .NET 6.0 SDK or later
- PowerShell or Command Prompt

## Running the Server

Follow these steps to run the InventoryService server:

1. Open a terminal/command prompt
2. Navigate to the project root directory:
   ```
   cd "C:\Users\AP37576\Async API"
   ```
3. Run the server using the following command:
   ```
   dotnet run --project InventoryService/InventoryService.csproj
   ```
4. The server will start and listen on http://localhost:5232
5. You should see output indicating that the server is running and the Kafka consumer service has started

## Running the Client (Normal Mode)

Follow these steps to run the SimpleClient in normal mode:

1. Open a new terminal/command prompt (keep the server running in the first terminal)
2. Navigate to the project root directory:
   ```
   cd "C:\Users\AP37576\Async API"
   ```
3. Run the client using the following command:
   ```
   dotnet run --project SimpleClient/SimpleClient.csproj
   ```
4. The client will:
   - Register a webhook with the server
   - Call the processInventory endpoint
   - Receive a 202 Accepted response
   - Wait for the webhook callback
   - Display the job details when the job completes

## Running the Client (Error Testing Mode)

To test different error scenarios, you can run the client in error testing mode:

1. Open a new terminal/command prompt (keep the server running in the first terminal)
2. Navigate to the project root directory:
   ```
   cd "C:\Users\AP37576\Async API"
   ```
3. Run the client in error testing mode using the following command:
   ```
   dotnet run --project SimpleClient/SimpleClient.csproj test-errors
   ```
4. The client will:
   - Run a normal successful job first
   - Then test all error scenarios one by one
   - Display a summary of all errors at the end

## Error Scenarios

The system demonstrates the following error scenarios:

1. **Validation Error**: Invalid inventory data (errorFlag=VALIDATION)
2. **Not Found Error**: Inventory item not found (errorFlag=NOTFOUND)
3. **Authorization Error**: Not authorized to process inventory (errorFlag=AUTH)
4. **Database Error**: Error connecting to the database (errorFlag=DB)
5. **Processing Error**: Error during inventory processing (errorFlag=PROCESSING)

To test a specific error scenario manually, you can call the API directly:

```
POST http://localhost:5232/api/inventory/processInventory?errorFlag=VALIDATION
```

## API Endpoints

The InventoryService exposes the following endpoints:

### Process Inventory
- **URL**: `/api/inventory/processInventory`
- **Method**: POST
- **Query Parameters**:
  - `errorFlag` (optional): Type of error to simulate (VALIDATION, NOTFOUND, AUTH, DB, PROCESSING)
- **Success Response**:
  - Code: 202 Accepted
  - Content: `{ "jobId": "guid-here" }`

### Register Webhook
- **URL**: `/api/inventory/registerWebhook`
- **Method**: POST
- **Request Body**:
  ```json
  {
    "callbackUrl": "http://localhost:8084/webhook"
  }
  ```
- **Success Response**:
  - Code: 200 OK
  - Content: `{ "webhookId": "guid-here" }`

## Webhook Callback Format

When a job completes, the server sends a POST request to the registered webhook URL with the following JSON payload:

```json
{
  "jobId": "guid-here",
  "createdAt": "2023-05-12T12:34:56.789Z",
  "completedAt": "2023-05-12T12:35:01.789Z",
  "status": "Completed",
  "result": "Inventory processing completed successfully"
}
```

For error scenarios, the payload includes error information:

```json
{
  "jobId": "guid-here",
  "createdAt": "2023-05-12T12:34:56.789Z",
  "completedAt": "2023-05-12T12:35:01.789Z",
  "status": "Error",
  "errorCode": "VALIDATION_ERROR",
  "errorMessage": "Invalid inventory data provided"
}
```

## Implementation Details

- The Kafka implementation is simulated in-memory for demonstration purposes
- The webhook service maintains a list of registered webhooks in memory
- The inventory processing service simulates a 5-second delay to represent a long-running task

## Troubleshooting

- If the client fails to receive webhook callbacks, ensure the server is running
- If you see connection errors, check that the ports (5232 for server, 8084 for client webhook) are available
- To restart the system, stop both the client and server (Ctrl+C) and start them again


