using System; // Provides fundamental types and basic functions
using System.Collections.Generic; // Provides generic collection classes and interfaces
using System.IO; // Provides classes for reading and writing to files and streams
using System.Linq; // Provides LINQ (Language Integrated Query) extension methods
using System.Net; // Provides networking functionality, such as working with IP addresses and sockets
using System.Net.Sockets; // Provides classes for working with sockets
using System.Threading; // Provides threading and synchronization primitives

class Servidor
{
    // Dictionaries to store services and tasks
    private static Dictionary<string, string> serviceDict = new Dictionary<string, string>();
    private static Dictionary<string, List<string>> taskDict = new Dictionary<string, List<string>>();
    // Mutex for thread synchronization
    private static Mutex mutex = new Mutex();

    static void Main(string[] args)
    {
        // Print the current working directory
        PrintWorkingDirectory();
        // Load tasks from CSV file
        LoadDataFromCSV();

        TcpListener servidor = null;
        try
        {
            // Start TCP server
            servidor = new TcpListener(IPAddress.Any, 1234);
            servidor.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            // Accept incoming client connections
            while (true)
            {
                TcpClient cliente = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado!");
                // Handle each client connection in a separate thread
                ThreadPool.QueueUserWorkItem(HandleClient, cliente);
            }
        }
        catch (SocketException ex)
        {
            // Handle socket exceptions
            Console.WriteLine("Erro de Socket: " + ex.ToString());
        }
        finally
        {
            // Stop the server when done
            if (servidor != null)
            {
                servidor.Stop();
            }
        }
    }

    private static void HandleClient(object obj)
    {
        // Handle each client connection
        TcpClient cliente = (TcpClient)obj;
        try
        {
            // Open network stream for reading and writing
            using (NetworkStream stream = cliente.GetStream())
            using (StreamReader leitor = new StreamReader(stream))
            using (StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true })
            {
                string mensagem;
                // Read messages from client
                while ((mensagem = leitor.ReadLine()) != null)
                {
                    Console.WriteLine("Mensagem recebida: " + mensagem);
                    // Process each message and generate a response
                    string resposta = ProcessMessage(mensagem);
                    escritor.WriteLine(resposta);
                }
            }
        }
        catch (IOException ex)
        {
            // Handle IO exceptions
            Console.WriteLine("Erro de E/S: " + ex.ToString());
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            Console.WriteLine("Erro inesperado: " + ex.ToString());
        }
        finally
        {
            // Close client connection
            if (cliente != null)
            {
                cliente.Close();
            }
        }
    }

    private static string ProcessMessage(string message)
    {
        try
        {
            // Process each message type
            if (message.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                // Handle CONNECT message
                return "100 OK";
            }
            else if (message.StartsWith("CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Handle CLIENT_ID message
                string clientId = message.Substring("CLIENT_ID:".Length).Trim();
                Console.WriteLine($"Received CLIENT_ID: {clientId}");
                return $"ID_CONFIRMED:{clientId}";
            }
            else if (message.StartsWith("TASK_COMPLETED:", StringComparison.OrdinalIgnoreCase))
            {
                // Handle TASK_COMPLETED message
                string taskDescription = message.Substring("TASK_COMPLETED:".Length).Trim();
                return MarkTaskAsCompleted(taskDescription);
            }
            else if (message.StartsWith("REQUEST_SERVICE CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Handle REQUEST_SERVICE message
                string clientId = message.Substring("REQUEST_SERVICE CLIENT_ID:".Length).Trim();
                return AllocateService(clientId);
            }
            else if (message.StartsWith("REQUEST_TASK CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                // Handle REQUEST_TASK message
                string clientId = message.Substring("REQUEST_TASK CLIENT_ID:".Length).Trim();
                return AllocateTask(clientId);
            }
            else if (message.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                // Handle SAIR message
                return "400 BYE";
            }
            else
            {
                // Handle unrecognized message
                return "500 ERROR: Comando não reconhecido";
            }
        }
        catch (Exception ex)
        {
            // Handle processing errors
            Console.WriteLine($"Error processing message: {ex}");
            return "500 ERROR: Internal server error";
        }
    }

    // Print current working directory
    private static void PrintWorkingDirectory()
    {
        string workingDirectory = Environment.CurrentDirectory;
        Console.WriteLine("Current Working Directory: " + workingDirectory);
    }

    // Load tasks from CSV file
    private static void LoadDataFromCSV()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        try
        {
            // Load tasks from CSV file
            if (File.Exists(taskFilePath))
            {
                // Clear existing task dictionary
                taskDict.Clear();

                // Read each line in the task file
                foreach (var line in File.ReadLines(taskFilePath).Skip(1))
                {
                    // Split the line into parts
                    var parts = line.Split(',');
                    // Ensure the line has at least three parts
                    if (parts.Length >= 3)
                    {
                        // Extract task ID, description, and status
                        var taskId = parts[0].Trim();
                        var taskDescription = parts[1].Trim();
                        var taskStatus = parts[2].Trim();

                        // Add task to the task dictionary
                        if (!taskDict.ContainsKey(taskId))
                        {
                            taskDict[taskId] = new List<string>();
                        }
                        taskDict[taskId].Add(taskDescription);
                    }
                }
                Console.WriteLine("Tarefas carregadas com sucesso."); // Success message
            }
            else
            {
                // Display an error message if the task file is not found
                Console.WriteLine($"Erro: Arquivo {taskFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            // Handle loading data errors
            Console.WriteLine($"Erro ao carregar dados dos arquivos CSV: {ex.Message}");
        }
    }

    // Allocate service to a client
    private static string AllocateService(string clientId)
    {
        if (serviceDict.ContainsKey(clientId))
        {
            // Allocate service to client
            string service = serviceDict[clientId];
            Console.WriteLine($"Alocando serviço '{service}' para o cliente {clientId}");
            return "SERVICE_ALLOCATED:" + service;
        }
        else
        {
            // No service available for client
            return "NO_SERVICE_AVAILABLE";
        }
    }

    // Update task status in CSV file
    private static void UpdateTaskCSV(string taskId, string newStatus, string clientId)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        if (File.Exists(taskFilePath))
        {
            var lines = File.ReadAllLines(taskFilePath).ToList();
            for (int i = 1; i < lines.Count; i++) // Start from index 1 to skip the header line
            {
                var parts = lines[i].Split(',');
                if (parts.Length >= 3 && parts[0].Trim() == taskId)
                {
                    parts[2] = newStatus;
                    if (parts.Length >= 4)
                    {                        // Update client ID if available
                        parts[3] = clientId;
                    }
                    else
                    {
                        // Add client ID field if it doesn't exist
                        Array.Resize(ref parts, parts.Length + 1);
                        parts[3] = clientId;
                    }
                    lines[i] = string.Join(",", parts);
                    File.WriteAllLines(taskFilePath, lines);
                    Console.WriteLine($"Tarefa '{taskId}' atualizada para '{newStatus}' com cliente '{clientId}' no arquivo CSV.");
                    return; // Exit the loop after updating the task
                }
            }
            Console.WriteLine($"Tarefa '{taskId}' não encontrada no arquivo CSV.");
        }
        else
        {
            // Handle file not found error
            Console.WriteLine($"Erro: Arquivo {taskFilePath} não encontrado.");
        }
    }

    // Check if a task is allocated to any client
    private static bool IsTaskAllocated(string taskId)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        if (File.Exists(taskFilePath))
        {
            var lines = File.ReadLines(taskFilePath).Skip(1);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4 && parts[0].Trim() == taskId && !string.IsNullOrEmpty(parts[3].Trim()))
                {
                    // Task is allocated to a client
                    return true;
                }
            }
        }

        // Task is not allocated to any client
        return false;
    }

    // Allocate task to a client
    private static string AllocateTask(string clientId)
    {
        mutex.WaitOne(); // Acquire mutex lock for thread safety
        try
        {
            // Check for unallocated tasks
            var unallocatedTasks = taskDict.Where(pair => pair.Value.Count > 0).ToList();
            if (unallocatedTasks.Any())
            {
                foreach (var taskPair in unallocatedTasks)
                {
                    var taskId = taskPair.Key;
                    var taskDescriptions = taskPair.Value;

                    // Check if any of the task descriptions are already allocated to a client
                    bool taskAllocated = taskDescriptions.Any(description => IsTaskAllocated(taskId, description));
                    if (taskAllocated)
                    {
                        continue; // Skip this task and check the next one
                    }

                    // Update the allocated task to the new user ID responsible for it
                    var newStatus = "Em curso";
                    UpdateTaskCSV(taskId, newStatus, clientId);

                    // Get the first unallocated task description
                    var taskDescription = taskDescriptions.First();

                    Console.WriteLine($"Alocando tarefa não alocada '{taskDescription}' para o cliente {clientId}");
                    return "TASK_ALLOCATED:" + taskDescription;
                }
            }

            // If no unallocated tasks are available or all tasks are already allocated, return a message indicating that
            return "NO_TASKS_AVAILABLE";
        }
        finally
        {
            mutex.ReleaseMutex(); // Release mutex lock after allocation
        }
    }

    // Check if a task with a specific description is allocated to any client
    private static bool IsTaskAllocated(string taskId, string taskDescription)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        if (File.Exists(taskFilePath))
        {
            var lines = File.ReadLines(taskFilePath).Skip(1);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4 && parts[0].Trim() == taskId && parts[1].Trim() == taskDescription && !string.IsNullOrEmpty(parts[3].Trim()))
                {
                    // Task is allocated to a client
                    return true;
                }
            }
        }

        // Task is not allocated to any client
        return false;
    }

    // Check if a task with a specific description is marked as completed
    private static bool IsTaskCompleted(string taskDescription)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        if (File.Exists(taskFilePath))
        {
            var lines = File.ReadAllLines(taskFilePath).Skip(1);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4 && parts[1].Trim().Equals(taskDescription.Trim(), StringComparison.OrdinalIgnoreCase) && parts[2].Trim().Equals("Concluido", StringComparison.OrdinalIgnoreCase))
                {
                    // Task is marked as completed
                    return true;
                }
            }
        }

        // Task is not marked as completed
        return false;
    }

    // Mark a task with a specific description as completed
    private static string MarkTaskAsCompleted(string taskDescription)
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

            if (File.Exists(taskFilePath))
            {
                var lines = File.ReadAllLines(taskFilePath);
                for (int i = 1; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 4 && parts[1].Trim().Equals(taskDescription.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Update task status to "Concluido"
                        parts[2] = "Concluido";
                        lines[i] = string.Join(",", parts);
                        File.WriteAllLines(taskFilePath, lines);
                        Console.WriteLine($"Tarefa '{taskDescription}' marcada como concluída.");
                        return "TASK_MARKED_COMPLETED";
                    }
                }
            }
            else
            {
                // Handle file not found error
                Console.WriteLine($"Erro: Arquivo {taskFilePath} não encontrado.");
            }

            return "TASK_NOT_FOUND"; // Task with the provided description not found
        }
        catch (Exception ex)
        {
            // Handle error when marking task as completed
            Console.WriteLine($"Erro ao marcar tarefa como concluída: {ex.Message}");
            return "500 ERROR: Internal server error";
        }
    }
}