using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ServidorUDP : MonoBehaviour
{
    [Header("Rede")]
    public int porta = 5001;

    [Header("Jogadores")]
    public int maximoJogadores = 4;

    private UdpClient servidor;
    private Thread threadReceber;

    private bool rodando = false;

    private Dictionary<string, int> clientes =
        new Dictionary<string, int>();

    private Dictionary<int, IPEndPoint> endpoints =
        new Dictionary<int, IPEndPoint>();

    private int proximoId = 1;

    void Start()
    {
        try
        {
            servidor = new UdpClient(porta);

            rodando = true;

            threadReceber =
                new Thread(ReceberDados);

            threadReceber.IsBackground = true;
            threadReceber.Start();

            Debug.Log(
                "Servidor iniciado na porta " + porta
            );
        }
        catch (Exception erro)
        {
            Debug.LogError(
                "Erro ao iniciar servidor: " +
                erro.Message
            );
        }
    }

    private void ReceberDados()
    {
        IPEndPoint cliente =
            new IPEndPoint(
                IPAddress.Any,
                0
            );

        while (rodando)
        {
            try
            {
                byte[] dados =
                    servidor.Receive(
                        ref cliente
                    );

                string mensagem =
                    Encoding.UTF8.GetString(
                        dados
                    );

                string chave =
                    cliente.Address +
                    ":" +
                    cliente.Port;

                if (mensagem == "HELLO")
                {
                    RegistrarCliente(
                        chave,
                        cliente
                    );
                }
            }
            catch (Exception erro)
            {
                if (rodando)
                {
                    Debug.LogError(
                        "Erro ao receber: " +
                        erro.Message
                    );
                }
            }
        }
    }

    private void RegistrarCliente(
        string chave,
        IPEndPoint cliente
    )
    {
        // Cliente já estava registrado
        if (clientes.ContainsKey(chave))
        {
            int idExistente =
                clientes[chave];

            EnviarMensagem(
                "ASSIGN:" + idExistente,
                cliente
            );

            return;
        }

        // Sala já possui 4 jogadores
        if (clientes.Count >= maximoJogadores)
        {
            EnviarMensagem(
                "SALA_CHEIA",
                cliente
            );

            Debug.Log(
                "Cliente recusado: sala cheia."
            );

            return;
        }

        int id =
            proximoId;

        proximoId++;

        clientes.Add(
            chave,
            id
        );

        endpoints.Add(
            id,
            new IPEndPoint(
                cliente.Address,
                cliente.Port
            )
        );

        Debug.Log(
            "Player " +
            id +
            " conectado -> " +
            chave
        );

        EnviarMensagem(
            "ASSIGN:" + id,
            cliente
        );

        if (clientes.Count == maximoJogadores)
        {
            Debug.Log(
                "4 jogadores conectados! Sala cheia."
            );
        }
    }

    private void EnviarMensagem(
        string mensagem,
        IPEndPoint destino
    )
    {
        byte[] dados =
            Encoding.UTF8.GetBytes(
                mensagem
            );

        servidor.Send(
            dados,
            dados.Length,
            destino
        );
    }

    private void OnApplicationQuit()
    {
        rodando = false;

        servidor?.Close();
    }
}