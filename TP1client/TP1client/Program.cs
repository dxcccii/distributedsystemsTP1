using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

class Cliente
{
    static void Main(string[] args)
    {
        Console.WriteLine("Bem-vindo à ServiMoto!");

        // Pedir endereço IP do servidor
        Console.Write("Por favor, insira o endereço IP do servidor: ");
        string enderecoServidor = Console.ReadLine();


        try
        {
            while (true) // Keep the client running indefinitely
            {
                // Conectar ao servidor
                using (TcpClient cliente = new TcpClient(enderecoServidor, 1234))
                using (NetworkStream stream = cliente.GetStream())
                using (StreamReader leitor = new StreamReader(stream))
                using (StreamWriter escritor = new StreamWriter(stream) { AutoFlush = true })
                {
                    Console.WriteLine("Conectado ao servidor. Aguardando resposta...");

                    // Enviar mensagem CONNECT para iniciar a comunicação
                    escritor.WriteLine("CONNECT");
                    string resposta = leitor.ReadLine();
                    Console.WriteLine("Resposta do servidor: " + resposta);

                    // Se a conexão foi estabelecida com sucesso, solicitar e enviar o ID do cliente
                    if (resposta == "100 OK")
                    {


                        Console.Write("Por favor, insira o seu ID de cliente: ");
                        string idCliente = Console.ReadLine();

                        // Enviar o ID do cliente para o servidor
                        escritor.WriteLine("CLIENT_ID:" + idCliente);

                        // Receber confirmação do servidor
                        resposta = leitor.ReadLine();
                        Console.WriteLine("Resposta " + resposta);


                        if (resposta.StartsWith("ID_CONFIRMED"))
                        {
                            while (true)
                            {
                                Console.WriteLine("1. Solicitar tarefa");
                                Console.WriteLine("2. Marcar tarefa como concluída");
                                Console.WriteLine("3. Sair");
                                Console.Write("Escolha uma opção: ");
                                string opcao = Console.ReadLine();

                                if (opcao == "1")
                                {
                                    // Solicitar nova tarefa
                                    escritor.WriteLine("REQUEST_TASK CLIENT_ID:" + idCliente);
                                    resposta = leitor.ReadLine();
                                    Console.WriteLine("Resposta do servidor: " + resposta);

                                    if (resposta.StartsWith("TASK_ALLOCATED"))
                                    {
                                        string descricaoTarefa = resposta.Substring("TASK_ALLOCATED:".Length).Trim();
                                        Console.WriteLine("Tarefa alocada: " + descricaoTarefa);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Não há tarefas disponíveis no momento.");
                                    }
                                }
                                else if (opcao == "2")
                                {
                                    // Marcar tarefa como concluída
                                    Console.Write("Por favor, insira a descrição da tarefa concluída: ");
                                    string descricaoTarefa = Console.ReadLine();
                                    escritor.WriteLine("TASK_COMPLETED: " + descricaoTarefa);
                                    resposta = leitor.ReadLine();
                                    Console.WriteLine("Resposta do servidor: " + resposta);
                                }
                                else if (opcao == "3")
                                {
                                    // Encerrar a comunicação
                                    escritor.WriteLine("SAIR");
                                    resposta = leitor.ReadLine();
                                    Console.WriteLine("Resposta do servidor: " + resposta);
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Opção inválida. Por favor, tente novamente.");
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Comunicação com o servidor encerrada.");
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine("Ocorreu um erro de E/S: " + ex.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ocorreu um erro: " + ex.Message);
        }
    }
}
