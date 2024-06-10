using System; // Basic .NET types and utilities
using System.Collections.Generic; // Provides generic collection classes
using System.IO; // Input/output operations
using System.Linq; // LINQ (Language-Integrated Query) for querying data
using System.Net; // Networking support
using System.Net.Sockets; // TCP/IP socket communication
using System.Threading; // Threading and synchronization primitives

class Servidor
{
    // Dictionary to store client services
    private static Dictionary<string, string> serviceDict = new Dictionary<string, string>();

    // Dictionary to store tasks assigned to clients
    private static Dictionary<string, List<string>> taskDict = new Dictionary<string, List<string>>();

    // Mutex to ensure thread safety when accessing shared resources
    private static Mutex mutex = new Mutex();

    // Main method
    static void Main(string[] args)
    {
        // Print the current working directory
        PrintWorkingDirectory();

        // Load data from CSV files into dictionaries
        LoadDataFromCSV();

        // Start TCP listener
        TcpListener servidor = null;

        try
        {
            // Initialize and start the TCP listener
            servidor = new TcpListener(IPAddress.Any, 1234);
            servidor.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            // Accept and handle client connections indefinitely
            while (true)
            {
                // Accept incoming client connections
                TcpClient cliente = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado!");

                // Delegate each client connection to a worker thread for concurrent handling
                ThreadPool.QueueUserWorkItem(HandleClient, cliente);
            }
        }
        catch (SocketException ex)
        {
            // Handle socket-related exceptions
            Console.WriteLine("Erro de Socket: " + ex.ToString());
        }
        finally
        {
            // Ensure that the TCP listener is stopped when exiting the server loop
            if (servidor != null)
            {
                servidor.Stop();
            }
        }
    }

    // Method to handle individual client connections
    private static void HandleClient(object obj)
    {
        TcpClient cliente = (TcpClient)obj;

        try
        {
            // Set up streams for reading from and writing to the client
            using (NetworkStream stream = cliente.GetStream())
            using (StreamReader leitor = new StreamReader(stream))
            using (StreamWriter escritor = new StreamWriter(stream)
            {
                AutoFlush = true
            })
            {
                string mensagem;

                // Continuously read messages from the client
                while ((mensagem = leitor.ReadLine()) != null)
                {
                    Console.WriteLine("Mensagem recebida: " + mensagem);
                    // Process the received message and generate a response
                    string resposta = ProcessMessage(mensagem);
                    // Send the response back to the client
                    escritor.WriteLine(resposta);
                }
            }
        }
        catch (IOException ex)
        {
            // Handle I/O-related exceptions
            Console.WriteLine("Erro de E/S: " + ex.ToString());
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            Console.WriteLine("Erro inesperado: " + ex.ToString());
        }
        finally
        {
            // Close the client connection when done
            if (cliente != null)
            {
                cliente.Close();
            }
        }
    }

    // Method to process messages received from clients
    // Process the incoming message and generate a response
    private static string ProcessMessage(string message)
    {
        try
        {
            // Check the type of message and perform corresponding actions
            if (message.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                return "100 OK"; // Respond with OK status
            }
            else if (message.StartsWith("CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract client ID from the message and confirm
                string clientId = message.Substring("CLIENT_ID:".Length).Trim();
                // Validate clientId if necessary
                Console.WriteLine( "Received CLIENT_ID: {clientId}");
                return "ID_CONFIRMED:{clientId}"; // Confirm client ID
            }
            else if (message.StartsWith("TASK_COMPLETED:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract task description from the message and mark it as completed
                string taskDescription = message.Substring("TASK_COMPLETED:".Length).Trim();
                return MarkTaskAsCompleted(taskDescription); // Mark task as completed
            }
            else if (message.StartsWith("REQUEST_SERVICE CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract client ID from the message and allocate a service
                string clientId = message.Substring("REQUEST_SERVICE CLIENT_ID:".Length).Trim();
                return AllocateService(clientId); // Allocate service to the client
            }
            else if (message.StartsWith("REQUEST_TASK CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Extract client ID from the message and allocate a task
                string clientId = message.Substring("REQUEST_TASK CLIENT_ID:".Length).Trim();
                return AllocateTask(clientId); // Allocate task to the client
            }
            else if (message.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                return "400 BYE"; // Respond with goodbye message
            }
            else
            {
                return "500 ERROR: Comando não reconhecido"; // Unknown command
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during message processing
            Console.WriteLine("Error processing message: {ex}");
            return "500 ERROR: Internal server error"; // Internal server error
        }
    }

    // Method to load data from CSV files into dictionaries
    // Method to print the current working directory
    private static void PrintWorkingDirectory()
    {
        string workingDirectory = Environment.CurrentDirectory;
        Console.WriteLine("Current Working Directory: " + workingDirectory);
    }

    // Method to load data from CSV files into dictionaries
    private static void LoadDataFromCSV()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string serviceFilePath = Path.Combine(baseDir, "Alocacao_Cliente_Servico.csv");
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        try
        {
            // Load services from the service CSV file
            if (File.Exists(serviceFilePath))
            {
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        // Add service information to the service dictionary
                        serviceDict[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                Console.WriteLine("Serviços carregados com sucesso."); // Success message
            }
            else
            {
                Console.WriteLine("Erro: Arquivo {serviceFilePath} não encontrado."); // Error message
            }

            // Load tasks

            // Load services from the specified CSV file
            if (File.Exists(serviceFilePath))
            {
                // Read each line in the service file, skipping the header (first line)
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    // Split the line into parts based on comma delimiter
                    var parts = line.Split(',');
                    // Ensure that the line has exactly two parts (service ID and service description)
                    if (parts.Length == 2)
                    {
                        // Trim whitespace and add service information to the service dictionary
                        serviceDict[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                Console.WriteLine("Serviços carregados com sucesso."); // Success message
            }
            else
            {
                // Display an error message if the service file is not found
                Console.WriteLine("Erro: Arquivo {serviceFilePath} não encontrado.");
            }

            // Load tasks

            // Load services from the specified CSV file
            if (File.Exists(serviceFilePath))
            {
                // Read each line in the service file, skipping the header (first line)
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    // Split the line into parts based on comma delimiter
                    var parts = line.Split(',');
                    // Ensure that the line has exactly two parts (service ID and service description)
                    if (parts.Length == 2)
                    {
                        // Trim whitespace and add service information to the service dictionary
                        serviceDict[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                Console.WriteLine("Serviços carregados com sucesso."); // Success message
            }
            else
            {
                // Display an error message if the service file is not found
                Console.WriteLine("Erro: Arquivo {serviceFilePath} não encontrado.");
            }

            // Load tasks from the specified CSV file
            if (File.Exists(taskFilePath))
            {
                // Read each line in the task file, skipping the header (first line)
                foreach (var line in File.ReadLines(taskFilePath).Skip(1))
                {
                    // Split the line into parts based on comma delimiter
                    var parts = line.Split(',');
                    // Ensure that the line has exactly four parts (task ID, description, status, and client ID)
                    if (parts.Length == 4)
                    {
                        // Extract task ID, description, and client ID
                        var taskId = parts[0].Trim();
                        var taskDescription = parts[1].Trim();
                        var clientId = parts[3].Trim(); // Assuming ClientID is in the fourth column
                                                        // Check if the client ID is empty, indicating an unassigned task
                        if (string.IsNullOrEmpty(clientId))
                        {
                            // Add the task description to the task dictionary under the corresponding task ID
                            if (!taskDict.ContainsKey(taskId))
                            {
                                taskDict[taskId] = new List<string>();
                            }
                            taskDict[taskId].Add(taskDescription);
                        }
                    }
                }
                Console.WriteLine("Tarefas carregadas com sucesso."); // Success message
            }
            else
            {
                // Display an error message if the task file is not found
                Console.WriteLine("Erro: Arquivo {taskFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao carregar dados dos arquivos CSV: {ex.Message}");
        }
    }

    // Method to allocate service to a client
    private static string AllocateService(string clientId)
    {
        if (serviceDict.ContainsKey(clientId))
        {
            // If the service for the client exists in the dictionary, allocate it
            string service = serviceDict[clientId];
            Console.WriteLine("Alocando serviço '{service}' para o cliente {clientId}");
            return "SERVICE_ALLOCATED:" + service;
        }
        else
        {
            // If no service is available for the client, return a message indicating that
            return "NO_SERVICE_AVAILABLE";
        }
    }

    // Method to allocate a task to a client
    private static string AllocateTask(string clientId)
    {
        mutex.WaitOne(); // Acquire mutex lock to ensure thread safety
        try
        {
            // Check if there are unallocated tasks
            var unallocatedTasks = taskDict.Where(pair => pair.Value.Count > 0).ToList();
            if (unallocatedTasks.Any())
            {
                // Get the first unallocated task and allocate it to the client
                var taskPair = unallocatedTasks.First();
                var taskId = taskPair.Key;
                var taskDescription = taskPair.Value.First();

                // Remove the allocated task from the task dictionary
                taskPair.Value.RemoveAt(0);

                Console.WriteLine("Alocando tarefa não alocada '{taskDescription}' para o cliente {clientId}");
                return "TASK_ALLOCATED:" + taskDescription;
            }
            else
            {
                // If no unallocated tasks are available, check if there are tasks assigned to the specific client
                if (taskDict.ContainsKey(clientId) && taskDict[clientId].Count > 0)
                {
                    // Get the first available task for the client and allocate it
                    var taskDescription = taskDict[clientId][0];
                    // Remove the allocated task from the task dictionary
                    taskDict[clientId].RemoveAt(0);

                    Console.WriteLine("Alocando tarefa '{taskDescription}' para o cliente {clientId}");
                    return "TASK_ALLOCATED:" + taskDescription;
                }
                else
                {
                    // If no tasks are available for the client, return a message indicating that
                    return "NO_TASKS_AVAILABLE";
                }
            }
        }
        finally
        {
            mutex.ReleaseMutex(); // Release mutex lock after allocation
        }
    }

    // Method to mark a task as completed
    private static string MarkTaskAsCompleted(string taskDescription)
    {
        try
        {
            // Logic to mark the task as completed (e.g., updating a database or a list)
            Console.WriteLine("Tarefa '{taskDescription}' marcada como concluída.");

            // Write the updated task information back to the CSV file
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

            Console.WriteLine("Task file path: {taskFilePath}");

            if (File.Exists(taskFilePath))
            {
                // Read all lines from the CSV file
                var lines = File.ReadAllLines(taskFilePath);
                for (int i = 1; i < lines.Length; i++) // Start from index 1 to skip the header line
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 4 && parts[1].Trim().Equals(taskDescription.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Update the task status to "Concluído" if the task description matches
                        parts[2] = "Concluido";
                        // Join the parts back into a line and update the CSV file
                        lines[i] = string.Join(",", parts);
                        File.WriteAllLines(taskFilePath, lines);
                        Console.WriteLine("Tarefa '{taskDescription}' atualizada como concluída no arquivo CSV.");
                        return "TASK_MARKED_COMPLETED";
                    }
                }
            }
            else
            {
                // If the CSV file doesn't exist, log an error message
                Console.WriteLine("Erro: Arquivo {taskFilePath} não encontrado.");
            }

            // If the task description is not found, return a message indicating that
            return "TASK_NOT_FOUND";
        }
        catch (Exception ex)
        {
            // If an exception occurs during the marking process, log the error message
            Console.WriteLine("Erro ao marcar tarefa como concluída: {ex.Message}");
            return "500 ERROR: Internal server error";
        }
    }
}