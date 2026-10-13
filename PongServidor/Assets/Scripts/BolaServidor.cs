using UnityEngine;

public class BolaServidor : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 7f;
    public float aumentoVelocidade = 0.35f;
    public float velocidadeMaxima = 13f;

    private Rigidbody2D rb;
    private ServidorUDP servidorUDP;

    private bool partidaIniciada = false;

    void Start()
    {
        rb =
            GetComponent<Rigidbody2D>();

        servidorUDP =
            FindFirstObjectByType<ServidorUDP>();

        rb.linearVelocity =
            Vector2.zero;

        transform.position =
            Vector3.zero;
    }

    public void IniciarPartida()
    {
        if (partidaIniciada)
            return;

        partidaIniciada = true;

        IniciarMovimento();
    }

    private void IniciarMovimento()
    {
        Vector2 direcao =
            new Vector2(
                1f,
                0.5f
            ).normalized;

        rb.linearVelocity =
            direcao * velocidade;
    }

    private void OnCollisionEnter2D(
        Collision2D colisao
    )
    {
        if (!partidaIniciada)
            return;

        if (
            colisao.gameObject.name == "Raquete1" ||
            colisao.gameObject.name == "Raquete2" ||
            colisao.gameObject.name == "Raquete3" ||
            colisao.gameObject.name == "Raquete4"
        )
        {
            float velocidadeAtual =
                rb.linearVelocity.magnitude;

            float novaVelocidade =
                Mathf.Min(
                    velocidadeAtual +
                    aumentoVelocidade,
                    velocidadeMaxima
                );

            rb.linearVelocity =
                rb.linearVelocity.normalized *
                novaVelocidade;
        }
    }

    private void OnTriggerEnter2D(
        Collider2D outro
    )
    {
        if (!partidaIniciada)
            return;

        if (outro.gameObject.name == "GolDireita")
        {
            servidorUDP.MarcarPontoTime1();

            if (!servidorUDP.JogoFinalizado)
            {
                ResetarBola(1);
            }
            else
            {
                PararBola();
            }
        }
        else if (
            outro.gameObject.name == "GolEsquerda"
        )
        {
            servidorUDP.MarcarPontoTime2();

            if (!servidorUDP.JogoFinalizado)
            {
                ResetarBola(-1);
            }
            else
            {
                PararBola();
            }
        }
    }

    private void ResetarBola(
        int direcaoX
    )
    {
        transform.position =
            Vector3.zero;

        Vector2 direcao =
            new Vector2(
                direcaoX,
                Random.Range(
                    -0.5f,
                    0.5f
                )
            ).normalized;

        rb.linearVelocity =
            direcao * velocidade;
    }

    private void PararBola()
    {
        rb.linearVelocity =
            Vector2.zero;
    }
}