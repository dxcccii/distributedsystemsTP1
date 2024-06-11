using System; // Provides basic functionalities like console input and output
using System.Collections.Generic; // Collections.Generic namespace provides classes that define generic collections
using System.IO; // Provides classes for reading and writing to files
using System.Linq; // Provides classes and interfaces that support queries that use Language-Integrated Query (LINQ).
using System.Net; // Provides a simple programming interface for many of the protocols used on networks today, such as HTTP, FTP, and SMTP.
using System.Net.Sockets; // Provides classes for creating TCP/IP client and server applications
using System.Threading; // Provides classes and interfaces that enable multithreaded programming.

class Servidor
{
    // Dictionary to store the mapping between client IDs and their allocated services
    private static Dictionary<string, string> serviceDict = new Dictionary<string, string>();

    // Dictionary to store the tasks for each service, mapping task IDs to their descriptions
    private static Dictionary<string, List<string>> taskDict = new Dictionary<string, List<string>>();

    // Mutex to ensure thread safety when accessing shared resources
    private static Mutex mutex = new Mutex();

    static void Main(string[] args)
    {
        // Print the current working directory for debugging purposes
        PrintWorkingDirectory();

        // Load service allocations from a CSV file
        LoadServiceAllocationsFromCSV();

        // Load tasks for all services from their respective CSV files
        LoadDataFromCSVForAllServices();

        TcpListener servidor = null;
        try
        {
            // Initialize the TCP listener on port 1234
            servidor = new TcpListener(IPAddress.Any, 1234);
            servidor.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            // Infinite loop to accept client connections
            while (true)
            {
                // Accept an incoming client connection
                TcpClient cliente = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado!");

                // Handle the client connection in a separate thread using the thread pool
                ThreadPool.QueueUserWorkItem(HandleClient, cliente);
            }
        }
        catch (SocketException ex)
        {
            // Log any socket exceptions
            Console.WriteLine("Erro de Socket: " + ex.ToString());
        }
        finally
        {
            // Stop the TCP listener if it was initialized
            if (servidor != null)
            {
                servidor.Stop();
            }
        }
    }

    private static void HandleClient(object obj)
    {
        TcpClient cliente = (TcpClient)obj;
        try
        {
            // Create network stream, reader, and writer for the client connection
            using (NetworkStream stream = cliente.GetStream())
            using (StreamReader leitor = new StreamReader(stream))
            using (StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true })
            {
                string mensagem;
                // Read messages from the client and process them
                while ((mensagem = leitor.ReadLine()) != null)
                {
                    Console.WriteLine("Mensagem recebida: " + mensagem);
                    string resposta = ProcessMessage(mensagem);
                    // Send the response back to the client
                    escritor.WriteLine(resposta);
                }
            }
        }
        catch (IOException ex)
        {
            // Log any I/O exceptions
            Console.WriteLine("Erro de E/S: " + ex.ToString());
        }
        catch (Exception ex)
        {
            // Log any unexpected exceptions
            Console.WriteLine("Erro inesperado: " + ex.ToString());
        }
        finally
        {
            // Close the client connection
            if (cliente != null)
            {
                cliente.Close();
            }
        }
    }

    // Method to process incoming client messages
    private static string ProcessMessage(string message)
    {
        try
        {
            // Check if the message starts with "CONNECT"
            if (message.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                // Respond with a confirmation code
                return "100 OK";
            }
            // Check if the message starts with "CLIENT_ID:"
            else if (message.StartsWith("CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the client ID from the message
                string clientId = message.Substring("CLIENT_ID:".Length).Trim();
                Console.WriteLine($"Received CLIENT_ID: {clientId}");
                // Respond with confirmation of the client ID
                return $"ID_CONFIRMED:{clientId}";
            }
            // Check if the message starts with "TASK_COMPLETED:"
            else if (message.StartsWith("TASK_COMPLETED:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the task description from the message
                string taskDescription = message.Substring("TASK_COMPLETED:".Length).Trim();

                // Get the client ID associated with the current connection
                string clientId = GetClientIdFromMessage(message);

                // Mark the task as completed and return the response
                return MarkTaskAsCompleted(clientId, taskDescription);
            }
            // Check if the message starts with "REQUEST_SERVICE CLIENT_ID:"
            else if (message.StartsWith("REQUEST_SERVICE CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the client ID from the message
                string clientId = message.Substring("REQUEST_SERVICE CLIENT_ID:".Length).Trim();
                // Allocate a service to the client and return the response
                return AllocateService(clientId);
            }
            // Check if the message starts with "REQUEST_TASK CLIENT_ID:"
            else if (message.StartsWith("REQUEST_TASK CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the client ID from the message
                string clientId = message.Substring("REQUEST_TASK CLIENT_ID:".Length).Trim();
                // Allocate a task to the client and return the response
                return AllocateTask(clientId);
            }
            // Check if the message is "SAIR" (exit)
            else if (message.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                // Respond with a disconnection code
                return "400 BYE";
            }
            else
            {
                // Respond with an error if the command is not recognized
                return "500 ERROR: Comando não reconhecido";
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during message processing
            Console.WriteLine($"Error processing message: {ex}");
            // Respond with a generic internal server error message
            return "500 ERROR: Internal server error";
        }
    }

    private static string GetClientIdFromMessage(string message)
    {
        string[] parts = message.Split(':');
        if (parts.Length >= 2)
        {
            return parts[1].Trim();
        }
        return string.Empty;
    }

    private static void PrintWorkingDirectory()
    {
        // Print the current working directory to the console
        string workingDirectory = Environment.CurrentDirectory;
        Console.WriteLine("Current Working Directory: " + workingDirectory);
    }

    // Method to load service allocations from a CSV file
    private static void LoadServiceAllocationsFromCSV()
    {
        // Define the file path for the service allocation CSV
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string serviceAllocationFilePath = Path.Combine(baseDir, "Alocacao_Cliente_Servico.csv");

        try
        {
            // Check if the CSV file exists
            if (File.Exists(serviceAllocationFilePath))
            {
                // Clear the service dictionary to ensure a clean start
                serviceDict.Clear();

                // Read each line from the CSV file, skipping the header
                foreach (var line in File.ReadLines(serviceAllocationFilePath).Skip(1))
                {
                    // Split the line into parts based on comma separator
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        // Extract client ID and service ID from the parts
                        var clientId = parts[0].Trim();
                        var serviceId = parts[1].Trim();

                        // Populate the dictionary with client-service mappings
                        serviceDict[clientId] = serviceId;
                    }
                }
                // Log successful loading of services from the CSV file
                Console.WriteLine("Serviços carregados com sucesso.");
            }
            else
            {
                // Log an error if the CSV file doesn't exist
                Console.WriteLine($"Erro: Arquivo {serviceAllocationFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors encountered during CSV file loading
            Console.WriteLine($"Erro ao carregar dados dos arquivos CSV: {ex.Message}");
        }
    }

    // Method to allocate a service to a client based on their client ID
    private static string AllocateService(string clientId)
    {
        // Check if the client has a service allocated
        if (serviceDict.ContainsKey(clientId))
        {
            // Get the service ID allocated to the client and log the allocation
            string service = serviceDict[clientId];
            Console.WriteLine($"Alocando serviço '{service}' para o cliente {clientId}");
            return "SERVICE_ALLOCATED:" + service;
        }
        else
        {
            // If the client has no allocated service, return a message indicating no service is available
            return "NO_SERVICE_AVAILABLE";
        }
    }

    // Method to load data from CSV files for all services
    private static void LoadDataFromCSVForAllServices()
    {
        // Iterate through each service in the service dictionary for debugging purposes
        foreach (var serviceId in serviceDict.Values)
        {
            // Define the file path for the service's tasks CSV
            string serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{serviceId}.csv");
            Console.WriteLine($"Loading tasks for service '{serviceId}' from {serviceFilePath}");

            // Load tasks for the current service from its respective CSV file for debugging purposes
            LoadDataFromCSV(serviceFilePath);
        }
    }

    // Method to load data from a CSV file for a specific service
    private static void LoadDataFromCSV(string serviceFilePath)
    {
        try
        {
            // Check if the CSV file exists
            if (File.Exists(serviceFilePath))
            {
                // Clear the task dictionary to ensure a clean start
                taskDict.Clear();

                // Read each line from the CSV file, skipping the header
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    // Log the processing of each line for debugging purposes
                    Console.WriteLine($"Processing line: {line}");

                    // Split the line into parts based on comma separator
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        // Extract task details from the parts
                        var taskId = parts[0].Trim();
                        var taskDescription = parts[1].Trim();
                        var taskStatus = parts[2].Trim();

                        // Extract optional client ID if available
                        var clientId = parts.Length > 3 ? parts[3].Trim() : null;

                        // Log task details for debugging purposes
                        Console.WriteLine($"Task ID: {taskId}, Description: {taskDescription}, Status: {taskStatus}, Client ID: {clientId}");

                        // Check if the task is unallocated
                        if (taskStatus.Equals("Nao alocada", StringComparison.OrdinalIgnoreCase))
                        {
                            // If the task is unallocated, add it to the task dictionary
                            if (!taskDict.ContainsKey(taskId))
                            {
                                taskDict[taskId] = new List<string>();
                                Console.WriteLine($"Created new task entry for ID: {taskId}");
                            }
                            taskDict[taskId].Add(taskDescription);
                            Console.WriteLine($"Added task '{taskDescription}' to taskDict under ID: {taskId}");
                        }
                        else
                        {
                            // If the task is already allocated, skip it
                            Console.WriteLine($"Task {taskId} is already allocated to client {clientId}. Skipping.");
                        }
                    }
                }
                // Log successful loading of tasks from the CSV file
                Console.WriteLine($"Tarefas carregadas com sucesso de {serviceFilePath}.");
            }
            else
            {
                // Log an error if the CSV file doesn't exist
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors encountered during CSV file processing
            Console.WriteLine($"Erro ao carregar dados do arquivo CSV {serviceFilePath}: {ex.Message}");
        }
    }

    // Method to allocate a task to a client based on their client ID
    private static string AllocateTask(string clientId)
    {
        // Ensure thread safety using a mutex
        mutex.WaitOne();
        try
        {
            // Check if the client has a service allocated
            if (!serviceDict.ContainsKey(clientId))
            {
                // If the client has no allocated service, return a message indicating no service is available
                return "NO_SERVICE_AVAILABLE";
            }

            // Get the service ID allocated to the client
            string service = serviceDict[clientId];
            Console.WriteLine($"Client {clientId} belongs to service {service}");

            // Define the file path for the service's tasks CSV
            string serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{service}.csv");
            Console.WriteLine($"Loading tasks from {serviceFilePath}");

            // Reload the tasks from the CSV file to get the latest state
            LoadDataFromCSV(serviceFilePath);

            // Log the number of tasks loaded from the CSV file for debugging purposes
            Console.WriteLine($"Found {taskDict.Count} tasks loaded from {serviceFilePath}");
            Console.WriteLine($"Verifying unallocated tasks for service '{service}'");

            // Iterate through the tasks to find an unallocated one
            foreach (var kvp in taskDict)
            {
                foreach (var taskDescription in kvp.Value)
                {
                    if (!IsTaskAllocated(serviceFilePath, kvp.Key, taskDescription))
                    {
                        // If the task is unallocated, allocate it to the client
                        Console.WriteLine($"Task '{taskDescription}' is unallocated. Allocating to client {clientId}.");
                        // Update the task's status to "Em curso" (in progress) and assign it to the client
                        UpdateTaskCSV(serviceFilePath, kvp.Key, "Em curso", clientId);
                        return $"TASK_ALLOCATED:{taskDescription}";
                    }
                    else
                    {
                        // If the task is already allocated, skip it
                        Console.WriteLine($"Task '{taskDescription}' is already allocated. Skipping.");
                    }
                }
            }

            // If no unallocated task is found, return a message indicating no task is available
            return "NO_TASK_AVAILABLE";
        }
        finally
        {
            // Release the mutex to allow other threads to access shared resources
            mutex.ReleaseMutex();
        }
    }

    // Method to check if a task is already allocated in a CSV file
    private static bool IsTaskAllocated(string serviceFilePath, string taskId, string taskDescription)
    {
        try
        {
            // Check if the CSV file exists
            if (File.Exists(serviceFilePath))
            {
                // Read each line from the CSV file, skipping the header
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    // Split the line into parts based on comma separator
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        // Extract task ID, task description, and task status from the parts
                        var loadedTaskId = parts[0].Trim();
                        var loadedTaskDescription = parts[1].Trim();
                        var loadedTaskStatus = parts[2].Trim();

                        // Check if the loaded task matches the specified task ID and task description
                        if (loadedTaskId == taskId && loadedTaskDescription == taskDescription)
                        {
                            // Task is considered allocated if its status is not "Nao alocada"
                            return !loadedTaskStatus.Equals("Nao alocada", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            else
            {
                // Log an error if the CSV file doesn't exist
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors encountered while checking task allocation
            Console.WriteLine($"Erro ao verificar se a tarefa está alocada: {ex.Message}");
        }

        // If the task is not found in the file, assume it is unallocated
        return false;
    }

    // Method to update the status of a task in the CSV file
    private static void UpdateTaskCSV(string serviceFilePath, string taskId, string newStatus, string clientId)
    {
        try
        {
            // Check if the CSV file exists
            if (File.Exists(serviceFilePath))
            {
                // Read all lines from the CSV file and store them in a list
                List<string> lines = File.ReadAllLines(serviceFilePath).ToList();

                // Iterate through each line starting from the second line (skipping the header)
                for (int i = 1; i < lines.Count; i++)
                {
                    // Split the line into parts based on comma separator
                    string[] parts = lines[i].Split(',');
                    if (parts.Length >= 3 && parts[0].Trim() == taskId)
                    {
                        // If the task ID matches, update the task's status and assigned client ID
                        lines[i] = $"{taskId},{parts[1]},{newStatus},{clientId}";

                        // Write the modified lines back to the CSV file
                        File.WriteAllLines(serviceFilePath, lines);
                        break;
                    }
                }
            }
            else
            {
                // If the CSV file doesn't exist, log an error message
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Log any errors encountered during CSV file updating
            Console.WriteLine($"Erro ao atualizar o arquivo CSV: {ex.Message}");
        }
    }

    private static string MarkTaskAsCompleted(string clientId, string taskDescription)
    {
        // Iterate through all services
        foreach (var serviceId in serviceDict.Values)
        {
            // Construct the file path for the service
            string serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{serviceId}.csv");

            try
            {
                if (File.Exists(serviceFilePath))
                {
                    // Read all lines from the service file
                    List<string> lines = File.ReadAllLines(serviceFilePath).ToList();

                    // Iterate through each line
                    foreach (string line in lines)
                    {
                        // Loop through all the lines in the file
                        for (int i = 1; i < lines.Count; i++)
                        {
                            string[] parts = lines[i].Split(',');
                            if (parts.Length >= 4 && parts[1].Trim() == taskDescription)
                            {
                                // Mark the task as completed
                                parts[2] = "Concluido";

                                // Update the line in the list of lines
                                lines[i] = string.Join(",", parts);

                                // Write the updated lines back to the file
                                File.WriteAllLines(serviceFilePath, lines);

                                // Return success message
                                return "TASK_MARKED_COMPLETED";
                            }
                        }

                    }

                }
                else
                {
                    // Service file not found
                    return $"ERROR_FILE_NOT_FOUND:{serviceFilePath}";
                }
            }
            catch (Exception ex)
            {
                // Error occurred while marking task as completed
                Console.WriteLine($"Error marking task as completed: {ex.Message}");
                return "ERROR_MARKING_TASK_COMPLETED";
            }
        }

        // Task description not found
        return "ERROR_TASK_NOT_FOUND";
    }

}