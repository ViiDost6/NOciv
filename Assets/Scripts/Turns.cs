using UnityEngine;
using UnityEngine.Events;
using TMPro;


public class Turns : MonoBehaviour
{
    public int turno;
    public TextMeshProUGUI texto;

    public UnityEvent OnTurnStartJ1;
    public UnityEvent OnTurnStartJ2;
    void Start()
    {
        turno = 1;
        //texto = No se como se pone desde codigo, esta puesto arrastrando.  CAMBIAR
    }


    public void IniciarTurno()
    {

        if (turno % 2 != 0)
        {
            OnTurnStartJ1?.Invoke();
        }
        else
            OnTurnStartJ2?.Invoke();
    }

    public void TerminarTurno()
    {
        turno++;
        if (turno % 2 != 0)
            texto.text = "Turno J1";
        else
        {
            texto.text = "Turno J2";
        }
            IniciarTurno();
    }

}
