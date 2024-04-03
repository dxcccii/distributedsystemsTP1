using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

class Client
{
    static void Main(string[] args)
    {
        Console.WriteLine("Bem-vindo a ServiMoto!");

        // pedir endereço IP do servidor
        Console.Write("Por favor, insira o endereço IP do servidor: ");
        string serverIP = Console.ReadLine();

        try
        {
            // conectar ao servidor

            TcpClient client = new TcpClient(serverIP, 1234);
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            // enviar mensagem CONNECT para iniciar a comunicação

            writer.WriteLine("CONNECT");
            Console.WriteLine("Conectado ao servidor. Aguardando resposta...");

            // receber a resposta do servidor

            string response = reader.ReadLine();
            Console.WriteLine("Resposta do servidor: " + response);

            // se a conexão foi estabelecida com sucesso, solicitar e enviar o ID do cliente

            if (response == "100 OK")
            {
                Console.Write("Por favor, insira seu ID de cliente: ");
                string clientID = Console.ReadLine();

                // enviar o ID do cliente para o servidor

                writer.WriteLine("CLIENT_ID: " + clientID);

                // receber confirmação do servidor

                response = reader.ReadLine();
                Console.WriteLine("Resposta do servidor: " + response);
            }

            // encerrar a comunicação 

            client.Close();
            Console.WriteLine("Comunicação com o servidor encerrada.");
        }
        catch (Exception ex)
        {
            // mensagem de erro

            Console.WriteLine("Ocorreu um erro: " + ex.Message);
        }
    }
}
