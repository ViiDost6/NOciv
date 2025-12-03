using UnityEngine;
using UnityEngine.SceneManagement;

public class PantallaPrincipal : MonoBehaviour
{
    public void Play()
    {
        CambiarEscena("UnitTest");
    }
    public void CambiarEscena(string escena)
    {
        SceneManager.LoadScene(escena);
    }
}
