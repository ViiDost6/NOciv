using System.Globalization;
using UnityEngine;
using TMPro;

public class ActualizacionRecursos : MonoBehaviour
{
    public TextMeshProUGUI textoRecursos;

    public void ActualizarRecursos(int recursos){
        textoRecursos.text = "Recursos: " + recursos;
    }
}
