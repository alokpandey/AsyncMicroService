using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // Flag to indicate if we're testing error scenarios
    public static bool IsTestingErrors { get; private set; } = false;

    // Collection to store errors
    private static readonly Dictionary<string, string> Errors = new Dictionary<string, string>();

    // Method to add an error to the collection
    public static void AddError(string errorCode, string errorMessage)
    {
        if (!Errors.ContainsKey(errorCode))
        {
            Errors[errorCode] = errorMessage;
        }
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting simple client...");

        // Define the server URL
        string serverUrl = "http://localhost:5232/api/inventory";
        int webhookPort = 8084; // Using a different port to avoid conflicts
        string webhookUrl = $"http://localhost:{webhookPort}/webhook";

        // Check if we should test error scenarios
        bool testErrors = args.Length > 0 && args[0].Equals("test-errors", StringComparison.OrdinalIgnoreCase);
        IsTestingErrors = testErrors;

        // Create HTTP client
        using var httpClient = new HttpClient();

        // Create and start webhook listener
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{webhookPort}/");
        listener.Start();

        Console.WriteLine($"Webhook listener started at {webhookUrl}");

        // Start listening for callbacks in the background
        var cts = new CancellationTokenSource();
        var listenTask = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();

                    if (context.Request.Url?.AbsolutePath == "/webhook")
                    {
                        using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                        var requestBody = await reader.ReadToEndAsync();

                        Console.WriteLine($"Received webhook callback: {requestBody}");

                        // Send response
                        var response = context.Response;
                        var responseString = "Webhook received";
                        var buffer = Encoding.UTF8.GetBytes(responseString);

                        response.ContentLength64 = buffer.Length;
                        response.StatusCode = 200;

                        var output = response.OutputStream;
                        await output.WriteAsync(buffer);
                        await output.FlushAsync();
                        output.Close();

                        // Parse the job
                        try
                        {
                            var job = JsonSerializer.Deserialize<InventoryJob>(requestBody);

                            if (job != null)
                            {
                                if (job.Status == "Error")
                                {
                                    Console.WriteLine("\nJob completed with error!");
                                    Console.WriteLine($"Job ID: {job.JobId}");
                                    Console.WriteLine($"Created at: {job.CreatedAt}");
                                    Console.WriteLine($"Completed at: {job.CompletedAt}");
                                    Console.WriteLine($"Status: {job.Status}");
                                    Console.WriteLine($"Error Code: {job.ErrorCode}");
                                    Console.WriteLine($"Error Message: {job.ErrorMessage}");

                                    // Store the error in a static collection to display later
                                    Program.AddError(job.ErrorCode, job.ErrorMessage);

                                    // Don't exit for error scenarios in test mode
                                    if (!Program.IsTestingErrors)
                                    {
                                        Console.WriteLine("\nError received via Kafka.");
                                        Console.WriteLine("Exiting immediately...");
                                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                                    }
                                }
                                else if (job.Status == "Completed")
                                {
                                    Console.WriteLine("\nJob completed successfully!");
                                    Console.WriteLine($"Job ID: {job.JobId}");
                                    Console.WriteLine($"Created at: {job.CreatedAt}");
                                    Console.WriteLine($"Completed at: {job.CompletedAt}");
                                    Console.WriteLine($"Status: {job.Status}");
                                    Console.WriteLine($"Result: {job.Result}");

                                    Console.WriteLine("\nSuccess! Async processing completed via Kafka.");

                                    // Don't exit for success scenarios in test mode
                                    if (!Program.IsTestingErrors)
                                    {
                                        Console.WriteLine("Exiting immediately...");
                                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing webhook data: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Only print errors that are not related to application exit
                if (!ex.Message.Contains("thread exit") &&
                    !ex.Message.Contains("application request") &&
                    !ex.Message.Contains("disposed object"))
                {
                    Console.WriteLine($"Error in webhook listener: {ex.Message}");
                }
            }
        });

        try
        {
            // Register webhook
            Console.WriteLine("Registering webhook with the server...");
            var registrationResponse = await httpClient.PostAsJsonAsync(
                $"{serverUrl}/registerWebhook",
                new { CallbackUrl = webhookUrl });

            registrationResponse.EnsureSuccessStatusCode();
            var registrationContent = await registrationResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Webhook registered successfully: {registrationContent}");

            // Call processInventory endpoint
            Console.WriteLine("\nCalling processInventory endpoint...");
            var inventoryResponse = await httpClient.PostAsync($"{serverUrl}/processInventory", null);

            Console.WriteLine($"Response status code: {(int)inventoryResponse.StatusCode} {inventoryResponse.StatusCode}");

            if (inventoryResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                var responseContent = await inventoryResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {responseContent}");

                // Wait for the webhook callback
                Console.WriteLine("\nWaiting for async process to complete...");

                if (IsTestingErrors)
                {
                    // Wait for the success case to complete
                    await Task.Delay(10000);

                    // Now test all error scenarios
                    Console.WriteLine("\n\n=== TESTING ERROR SCENARIOS ===\n");

                    string[] errorFlags = { "VALIDATION", "NOTFOUND", "AUTH", "DB", "PROCESSING" };

                    for (int i = 0; i < errorFlags.Length; i++)
                    {
                        string errorFlag = errorFlags[i];
                        Console.WriteLine($"\nTesting error scenario {i+1}/{errorFlags.Length}: {errorFlag}");

                        try
                        {
                            // Call the processInventory endpoint with the error flag
                            var errorResponse = await httpClient.PostAsync($"{serverUrl}/processInventory?errorFlag={errorFlag}", null);

                            Console.WriteLine($"Response status code: {(int)errorResponse.StatusCode} {errorResponse.StatusCode}");

                            if (errorResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
                            {
                                var errorResponseContent = await errorResponse.Content.ReadAsStringAsync();
                                Console.WriteLine($"Response content: {errorResponseContent}");

                                // Wait for the webhook callback
                                Console.WriteLine("Waiting for error response...");
                                await Task.Delay(8000); // Wait for the error to be processed
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error during test: {ex.Message}");
                        }

                        // Add a small delay between tests
                        if (i < errorFlags.Length - 1)
                        {
                            await Task.Delay(1000);
                        }
                    }

                    // Display all collected errors
                    Console.WriteLine("\n\n=== ERROR SUMMARY ===\n");

                    if (Errors.Count > 0)
                    {
                        foreach (var error in Errors)
                        {
                            Console.WriteLine($"Error Code: {error.Key}");
                            Console.WriteLine($"Error Message: {error.Value}");
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine("No errors were collected. Something went wrong with the tests.");
                    }
                }
                else
                {
                    // Wait for 15 seconds max in normal mode
                    await Task.Delay(15000);

                    // If we get here, the webhook didn't exit the program
                    Console.WriteLine("\nTimeout waiting for job completion.");
                }
            }
            else
            {
                Console.WriteLine("Expected 202 Accepted response from the server");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // Clean up
        cts.Cancel();
        listener.Stop();
        listener.Close();

        Console.WriteLine("Client execution completed.");
    }

    class InventoryJob
    {
        public string JobId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
