using UnityEngine;
using UnityEngine.SceneManagement;

public class PantallaPrincipal : MonoBehaviour
{
    public void Play()
    {
        CambiarEscena("Juego");
    }
    public void CambiarEscena(string escena)
    {
        SceneManager.LoadScene(escena);
    }
}
