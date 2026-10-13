using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
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

    [Header("Raquetes")]
    public Transform raquete1;
    public Transform raquete2;
    public Transform raquete3;
    public Transform raquete4;

    [Header("Bola")]
    public Transform bola;
    public float intervaloEnvioBola = 0.03f;

    [Header("Placar")]
    public int pontosTime1 = 0;
    public int pontosTime2 = 0;
    public int pontosParaVencer = 5;

    private UdpClient servidor;
    private Thread threadReceber;

    private bool rodando = false;
    private bool jogoFinalizado = false;

    private Dictionary<string, int> clientes =
        new Dictionary<string, int>();

    private Dictionary<int, IPEndPoint> endpoints =
        new Dictionary<int, IPEndPoint>();

    private int proximoId = 1;

    private float[] posicoesY =
        new float[5];

    private bool[] atualizarRaquete =
        new bool[5];

    private readonly object travaRaquetes =
        new object();

    private float proximoEnvioBola = 0f;

    private volatile bool iniciarPartidaPendente = false;
    private bool partidaIniciada = false;
    private volatile bool salaCheia = false;

    public bool JogoFinalizado
    {
        get
        {
            return jogoFinalizado;
        }
    }

    void Start()
    {
        Application.runInBackground = true;
        
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

    void Update()
    {
        AtualizarRaquetes();

        // Inicia a bola pela thread principal da Unity
        if (iniciarPartidaPendente &&
            !partidaIniciada)
        {
            iniciarPartidaPendente = false;
            partidaIniciada = true;

            BolaServidor bolaServidor =
                FindFirstObjectByType<BolaServidor>();

            if (bolaServidor != null)
            {
                bolaServidor.IniciarPartida();

                Debug.Log(
                    "Bola iniciada! Partida começou."
                );
            }
            else
            {
                Debug.LogError(
                    "BolaServidor não encontrado!"
                );
            }
        }

        if (salaCheia &&
            bola != null &&
            !jogoFinalizado &&
            Time.time >= proximoEnvioBola)
        {
            EnviarPosicaoBola();

            proximoEnvioBola =
                Time.time +
                intervaloEnvioBola;
        }
    }

    private void AtualizarRaquetes()
    {
        lock (travaRaquetes)
        {
            for (int id = 1; id <= 4; id++)
            {
                if (!atualizarRaquete[id])
                    continue;

                Transform raquete =
                    ObterRaquete(id);

                if (raquete != null)
                {
                    Vector3 posicao =
                        raquete.position;

                    posicao.y =
                        posicoesY[id];

                    raquete.position =
                        posicao;
                }

                atualizarRaquete[id] =
                    false;
            }
        }
    }

    private void EnviarPosicaoBola()
{
    string mensagem =
        "BOLA:" +
        bola.position.x.ToString(
            "F3",
            CultureInfo.InvariantCulture
        ) +
        ":" +
        bola.position.y.ToString(
            "F3",
            CultureInfo.InvariantCulture
        );

    Debug.Log(
        "SERVIDOR ENVIANDO: " +
        mensagem
    );

    EnviarParaTodos(
        mensagem
    );
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
                else if (
                    mensagem.StartsWith("RAQUETE:")
                )
                {
                    ReceberPosicaoRaquete(
                        chave,
                        mensagem
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

    private void ReceberPosicaoRaquete(
        string chave,
        string mensagem
    )
    {
        if (!clientes.ContainsKey(chave))
            return;

        if (jogoFinalizado)
            return;

        int id =
            clientes[chave];

        string yTexto =
            mensagem.Substring(8);

        float y;

        if (!float.TryParse(
            yTexto,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out y
        ))
        {
            return;
        }

        if (id == 1 || id == 3)
        {
            y = Mathf.Clamp(
                y,
                0.3f,
                4f
            );
        }
        else
        {
            y = Mathf.Clamp(
                y,
                -4f,
                -0.3f
            );
        }

        lock (travaRaquetes)
        {
            posicoesY[id] = y;
            atualizarRaquete[id] = true;
        }

        string mensagemBroadcast =
            "RAQUETE:" +
            id +
            ":" +
            y.ToString(
                "F3",
                CultureInfo.InvariantCulture
            );

        EnviarParaTodos(
            mensagemBroadcast
        );
    }

    public void MarcarPontoTime1()
    {
        if (jogoFinalizado)
            return;

        pontosTime1++;

        Debug.Log(
            "PLACAR: TIME 1 " +
            pontosTime1 +
            " x " +
            pontosTime2 +
            " TIME 2"
        );

        EnviarPlacar();

        if (pontosTime1 >=
            pontosParaVencer)
        {
            FinalizarJogo(1);
        }
    }

    public void MarcarPontoTime2()
    {
        if (jogoFinalizado)
            return;

        pontosTime2++;

        Debug.Log(
            "PLACAR: TIME 1 " +
            pontosTime1 +
            " x " +
            pontosTime2 +
            " TIME 2"
        );

        EnviarPlacar();

        if (pontosTime2 >=
            pontosParaVencer)
        {
            FinalizarJogo(2);
        }
    }

    private void EnviarPlacar()
    {
        EnviarParaTodos(
            "PLACAR:" +
            pontosTime1 +
            ":" +
            pontosTime2
        );
    }

    private void FinalizarJogo(
        int timeVencedor
    )
    {
        jogoFinalizado = true;

        Debug.Log(
            "TIME " +
            timeVencedor +
            " VENCEU!"
        );

        EnviarParaTodos(
            "VITORIA:" +
            timeVencedor
        );
    }

    private Transform ObterRaquete(
        int id
    )
    {
        switch (id)
        {
            case 1:
                return raquete1;

            case 2:
                return raquete2;

            case 3:
                return raquete3;

            case 4:
                return raquete4;
        }

        return null;
    }

    private void RegistrarCliente(
        string chave,
        IPEndPoint cliente
    )
    {
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

        if (clientes.Count ==
            maximoJogadores)
        {
            salaCheia = true;

            Debug.Log(
                "4 jogadores conectados! Sala cheia."
            );

            EnviarParaTodos(
                "INICIAR_PARTIDA"
            );

            Debug.Log(
                "INICIAR_PARTIDA enviado para todos!"
            );

            // Apenas avisa a thread principal
            iniciarPartidaPendente = true;
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

    private void EnviarParaTodos(
        string mensagem
    )
    {
        foreach (
            KeyValuePair<int, IPEndPoint> jogador
            in endpoints
        )
        {
            EnviarMensagem(
                mensagem,
                jogador.Value
            );
        }
    }

    private void OnApplicationQuit()
    {
        rodando = false;

        servidor?.Close();
    }
}