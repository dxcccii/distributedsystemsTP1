using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    static Dictionary<string, List<Task>> services = new Dictionary<string, List<Task>>();
    static readonly object fileLock = new object();

    static void Main(string[] args)
    {
        LoadServicesFromCSV();
        LoadClientAllocations();

        TcpListener server = new TcpListener(IPAddress.Any, 1234);
        server.Start();

        Console.WriteLine("Servidor iniciado. Aguardando conexões...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Cliente conectado.");

            // nova thread criada para lidar com cada cliente

            ThreadPool.QueueUserWorkItem(HandleClient, client);
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream);
        writer.AutoFlush = true;

        // resposta "100 OK" quando inicialmente contatado

        writer.WriteLine("100 OK");

        string message = reader.ReadLine();
        Console.WriteLine("Mensagem recebida: " + message);

        string response = ProcessMessage(message);
        Console.WriteLine("Resposta: " + response);
        writer.WriteLine(response);

        client.Close();
    }

    static string ProcessMessage(string message)
    {
        if (message.StartsWith("CONNECT", StringComparison.OrdinalIgnoreCase))
        {
            return "100 OK";
        }
        else if (message.StartsWith("CLIENT_ID:", StringComparison.OrdinalIgnoreCase))
        {
            // por codigo para atribuir o ID do cliente

            return "ID_CONFIRMED";
        }
        else if (message.StartsWith("TASK_COMPLETED:", StringComparison.OrdinalIgnoreCase))
        {
            // por codigo para a tarefa concluida

            return "TASK_COMPLETED_CONFIRMED";
        }
        else if (message.StartsWith("REQUEST_TASK", StringComparison.OrdinalIgnoreCase))
        {
            // codigo para para atribuir uma nova tarefa ao cliente (nao necessario?)
            // responder NO_TASK_AVAILABLE se nao houver tarefas disponíveis

            return "TASK_ALLOCATED: Descrição_da_Nova_Tarefa";
        }
        else if (message.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
        {
            return "400 BYE";
        }
        else
        {
            // resposta em caso de erro

            return "500 ERROR: Comando não reconhecido";
        }
    }

    static void LoadServicesFromCSV()
    {
        try
        {
            string[] files = Directory.GetFiles("Caminho_do_Diretório_CSV", "*.csv");
            foreach (string file in files)
            {
                string serviceName = Path.GetFileNameWithoutExtension(file);
                List<Task> tasks = new List<Task>();
                using (StreamReader sr = new StreamReader(file))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] fields = line.Split(',');
                        if (fields.Length >= 2)
                        {
                            string taskDescription = fields[0];
                            string taskState = fields[1];
                            Task task = new Task { Description = taskDescription, State = taskState };
                            tasks.Add(task);
                        }
                    }
                }
                services.Add(serviceName, tasks);
            }
            Console.WriteLine("Serviços carregados com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao carregar serviços: " + ex.Message);
        }
    }

    static void LoadClientAllocations()
    {
        // codigo para a alocação de clientes do ficheiro CSV para serviços
    }

    class Task
    {
        public string Description { get; set; } // descricao da tarefa
        public string State { get; set; } // opcoes: concluída, em curso ou não alocada
        public string AssignedClient { get; set; } // ID do cliente 
    }
}
