using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{   
    private static Dictionary<string, string> serviceDict = new Dictionary<string, string>();
    private static Dictionary<string, List<string>> taskDict = new Dictionary<string, List<string>>();
    private static Mutex mutex = new Mutex();

    static void Main(string[] args)
    {
        PrintWorkingDirectory();
        LoadServiceAllocationsFromCSV();
        LoadDataFromCSVForAllServices(); // Load data for all services

        TcpListener servidor = null;
        try
        {
            servidor = new TcpListener(IPAddress.Any, 1234);
            servidor.Start();
            Console.WriteLine("Servidor iniciado. Aguardando conexões...");

            while (true)
            {
                TcpClient cliente = servidor.AcceptTcpClient();
                Console.WriteLine("Cliente conectado!");
                ThreadPool.QueueUserWorkItem(HandleClient, cliente);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Erro de Socket: " + ex.ToString());
        }
        finally
        {
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
            using (NetworkStream stream = cliente.GetStream())
            using (StreamReader leitor = new StreamReader(stream))
            using (StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true })
            {
                string mensagem;
                while ((mensagem = leitor.ReadLine()) != null)
                {
                    Console.WriteLine("Mensagem recebida: " + mensagem);
                    string resposta = ProcessMessage(mensagem);
                    escritor.WriteLine(resposta);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine("Erro de E/S: " + ex.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro inesperado: " + ex.ToString());
        }
        finally
        {
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
            if (message.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                return "100 OK";
            }
            else if (message.StartsWith("CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                string clientId = message.Substring("CLIENT_ID:".Length).Trim();
                Console.WriteLine($"Received CLIENT_ID: {clientId}");
                return $"ID_CONFIRMED:{clientId}";
            }
            else if (message.StartsWith("TASK_COMPLETED:", StringComparison.OrdinalIgnoreCase))
            {
                string taskDescription = message.Substring("TASK_COMPLETED:".Length).Trim();
                return MarkTaskAsCompleted(taskDescription);
            }
            else if (message.StartsWith("REQUEST_SERVICE CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                string clientId = message.Substring("REQUEST_SERVICE CLIENT_ID:".Length).Trim();
                return AllocateService(clientId);
            }
            else if (message.StartsWith("REQUEST_TASK CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
            {
                string clientId = message.Substring("REQUEST_TASK CLIENT_ID:".Length).Trim();
                return AllocateTask(clientId);
            }
            else if (message.Equals("SAIR", StringComparison.OrdinalIgnoreCase))
            {
                return "400 BYE";
            }
            else
            {
                return "500 ERROR: Comando não reconhecido";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex}");
            return "500 ERROR: Internal server error";
        }
    }

    private static void PrintWorkingDirectory()
    {
        string workingDirectory = Environment.CurrentDirectory;
        Console.WriteLine("Current Working Directory: " + workingDirectory);
    }

    private static void LoadServiceAllocationsFromCSV()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string serviceAllocationFilePath = Path.Combine(baseDir, "Alocacao_Cliente_Servico.csv");

        try
        {
            if (File.Exists(serviceAllocationFilePath))
            {
                serviceDict.Clear();

                foreach (var line in File.ReadLines(serviceAllocationFilePath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        var clientId = parts[0].Trim();
                        var serviceId = parts[1].Trim();

                        serviceDict[clientId] = serviceId;
                    }
                }
                Console.WriteLine("Serviços carregados com sucesso.");
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {serviceAllocationFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar dados dos arquivos CSV: {ex.Message}");
        }
    }


    private static string AllocateService(string clientId)
    {
        if (serviceDict.ContainsKey(clientId))
        {
            string service = serviceDict[clientId];
            Console.WriteLine($"Alocando serviço '{service}' para o cliente {clientId}");
            return "SERVICE_ALLOCATED:" + service;
        }
        else
        {
            return "NO_SERVICE_AVAILABLE";
        }
    }

    private static void LoadDataFromCSVForAllServices()
    {
        foreach (var serviceId in serviceDict.Values)
        {
            string serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{serviceId}.csv");
            Console.WriteLine($"Loading tasks for service '{serviceId}' from {serviceFilePath}");
            LoadDataFromCSV(serviceFilePath);
        }
    }


    private static void LoadDataFromCSV(string serviceFilePath)
    {
        try
        {
            if (File.Exists(serviceFilePath))
            {
                taskDict.Clear(); // Clear the dictionary before loading new data

                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    Console.WriteLine($"Processing line: {line}");

                    var parts = line.Split(',');
                    if (parts.Length >= 3) // Change from 4 to 3
                    {
                        var taskId = parts[0].Trim();
                        var taskDescription = parts[1].Trim();
                        var taskStatus = parts[2].Trim();
                        var clientId = parts.Length > 3 ? parts[3].Trim() : null; // Handle optional client ID

                        Console.WriteLine($"Task ID: {taskId}, Description: {taskDescription}, Status: {taskStatus}, Client ID: {clientId}");

                        // Debug print statement to verify taskId and taskStatus
                        Console.WriteLine($"Checking if task is unallocated: Task ID: {taskId}, Status: {taskStatus}");

                        // Task is unallocated
                        if (taskStatus.Equals("Nao alocada", StringComparison.OrdinalIgnoreCase))
                        {
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
                            Console.WriteLine($"Task {taskId} is already allocated to client {clientId}. Skipping.");
                        }
                    }
                }

                Console.WriteLine($"Tarefas carregadas com sucesso de {serviceFilePath}.");
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar dados do arquivo CSV {serviceFilePath}: {ex.Message}");
        }
    }




    private static string AllocateTask(string clientId)
    {
        mutex.WaitOne();
        try
        {
            if (!serviceDict.ContainsKey(clientId))
            {
                return "NO_SERVICE_AVAILABLE";
            }

            string service = serviceDict[clientId];
            Console.WriteLine($"Client {clientId} belongs to service {service}");

            string serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{service}.csv");
            Console.WriteLine($"Loading tasks from {serviceFilePath}");

            LoadDataFromCSV(serviceFilePath);

            Console.WriteLine($"Found {taskDict.Count} tasks loaded from {serviceFilePath}");

            Console.WriteLine($"Verifying unallocated tasks for service '{service}'");

            foreach (var kvp in taskDict)
            {
                foreach (var taskDescription in kvp.Value)
                {
                    if (!IsTaskAllocated(serviceFilePath, kvp.Key, taskDescription))
                    {
                        Console.WriteLine($"Task '{taskDescription}' is unallocated. Allocating to client {clientId}.");
                        UpdateTaskCSV(serviceFilePath, kvp.Key, "Em curso", clientId);
                        return $"TASK_ALLOCATED:{taskDescription}";
                    }
                    else
                    {
                        Console.WriteLine($"Task '{taskDescription}' is already allocated. Skipping.");
                    }
                }
            }

            return "NO_TASK_AVAILABLE";
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }


    private static bool IsTaskAllocated(string serviceFilePath, string taskId, string taskDescription)
    {
        try
        {
            if (File.Exists(serviceFilePath))
            {
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3)
                    {
                        var loadedTaskId = parts[0].Trim();
                        var loadedTaskDescription = parts[1].Trim();
                        var loadedTaskStatus = parts[2].Trim();

                        if (loadedTaskId == taskId && loadedTaskDescription == taskDescription)
                        {
                            // Task is unallocated if status is "Nao alocada"
                            return !loadedTaskStatus.Equals("Nao alocada", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao verificar se a tarefa está alocada: {ex.Message}");
        }

        // If task not found in the file, assume it is unallocated
        return false;
    }


    private static void UpdateTaskCSV(string serviceFilePath, string taskId, string newStatus, string clientId)
    {
        try
        {
            if (File.Exists(serviceFilePath))
            {
                List<string> lines = File.ReadAllLines(serviceFilePath).ToList();
                for (int i = 1; i < lines.Count; i++)
                {
                    string[] parts = lines[i].Split(',');
                    if (parts.Length >= 3 && parts[0].Trim() == taskId)
                    {
                        lines[i] = $"{taskId},{parts[1]},{newStatus},{clientId}";
                        File.WriteAllLines(serviceFilePath, lines);
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar o arquivo CSV: {ex.Message}");
        }
    }

    private static string MarkTaskAsCompleted(string taskDescription)
    {
        // Implement marking a task as completed
        return "TASK_MARKED_COMPLETED";
    }
}
