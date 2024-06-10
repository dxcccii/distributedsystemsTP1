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
    private static object lockObject = new object();

    static void Main(string[] args)
    {
        PrintWorkingDirectory();
        LoadDataFromCSV();

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
                // Validate clientId if necessary
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

    private static void LoadDataFromCSV()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string serviceFilePath = Path.Combine(baseDir, "Alocacao_Cliente_Servico.csv");
        string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

        try
        {
            // Load services
            if (File.Exists(serviceFilePath))
            {
                foreach (var line in File.ReadLines(serviceFilePath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        serviceDict[parts[0].Trim()] = parts[1].Trim();
                    }
                }
                Console.WriteLine("Serviços carregados com sucesso.");
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {serviceFilePath} não encontrado.");
            }

            // Load tasks
            if (File.Exists(taskFilePath))
            {
                foreach (var line in File.ReadLines(taskFilePath).Skip(1))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 4)
                    {
                        var taskId = parts[0].Trim();
                        var taskDescription = parts[1].Trim();
                        var clientId = parts[3].Trim(); // Assuming ClientID is in the fourth column
                        if (string.IsNullOrEmpty(clientId))
                        {
                            if (!taskDict.ContainsKey(taskId))
                            {
                                taskDict[taskId] = new List<string>();
                            }
                            taskDict[taskId].Add(taskDescription);
                        }
                    }
                }
                Console.WriteLine("Tarefas carregadas com sucesso.");
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {taskFilePath} não encontrado.");
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

    private static string AllocateTask(string clientId)
    {
        lock (lockObject)
        {
            // Check if there are unallocated tasks
            var unallocatedTasks = taskDict.Where(pair => pair.Value.Count > 0).ToList();
            if (unallocatedTasks.Any())
            {
                // Get the first unallocated task
                var taskPair = unallocatedTasks.First();
                var taskId = taskPair.Key;
                var taskDescription = taskPair.Value.First();

                // Remove the allocated task from the taskDict
                taskPair.Value.RemoveAt(0);

                Console.WriteLine($"Alocando tarefa não alocada '{taskDescription}' para o cliente {clientId}");
                return "TASK_ALLOCATED:" + taskDescription;
            }
            else
            {
                // Check if there are tasks assigned to the specific client
                if (taskDict.ContainsKey(clientId) && taskDict[clientId].Count > 0)
                {
                    // Get the first available task for the client
                    var taskDescription = taskDict[clientId][0];
                    // Remove the allocated task from the taskDict
                    taskDict[clientId].RemoveAt(0);

                    Console.WriteLine($"Alocando tarefa '{taskDescription}' para o cliente {clientId}");
                    return "TASK_ALLOCATED:" + taskDescription;
                }
                else
                {
                    return "NO_TASKS_AVAILABLE";
                }
            }
        }
    }

    private static string MarkTaskAsCompleted(string taskDescription)
    {
        try
        {
            // Logic to mark the task as completed (e.g., updating a database or a list)
            Console.WriteLine($"Tarefa '{taskDescription}' marcada como concluída.");

            // Write the updated task information back to the CSV file
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string taskFilePath = Path.Combine(baseDir, "Servico_A.csv");

            Console.WriteLine($"Task file path: {taskFilePath}");

            if (File.Exists(taskFilePath))
            {
                var lines = File.ReadAllLines(taskFilePath);
                for (int i = 1; i < lines.Length; i++) // Start from index 1 to skip the header line
                {
                    var parts = lines[i].Split(',');
                    if (parts.Length == 4 && parts[1].Trim().Equals(taskDescription.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Update the task status to "Concluído"
                        parts[2] = "Concluido";
                        lines[i] = string.Join(",", parts);
                        File.WriteAllLines(taskFilePath, lines);
                        Console.WriteLine($"Tarefa '{taskDescription}' atualizada como concluída no arquivo CSV.");
                        return "TASK_MARKED_COMPLETED";
                    }
                }
            }
            else
            {
                Console.WriteLine($"Erro: Arquivo {taskFilePath} não encontrado.");
            }

            return "TASK_NOT_FOUND";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao marcar tarefa como concluída: {ex.Message}");
            return "500 ERROR: Internal server error";
        }
    }



}

