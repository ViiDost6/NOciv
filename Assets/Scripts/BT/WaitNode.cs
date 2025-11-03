using UnityEngine;

[System.Serializable]
public class WaitNode : Node
{
    [ShowInEditor]
    public float duration = 1.0f;
    private float startTime; // Para saber cuando empieza
    public override NodeState Evaluate(GameObject agent)
    {
        //Si no estábamos en running, es la primera ejecución
        if (state != NodeState.Running)
        {
            state = NodeState.Running;
            startTime = Time.time;
            return state;
        }

        //Si se acaba el tiempo, la acción ha tenido éxito.
        if (Time.time - startTime >= duration)
        {
            state = NodeState.Success;
            return state;
        }
        //Si está aquí aún no ha pasado el tiempo suficiente.
        // Se sigue ejecutanto (running)
        state = NodeState.Running;
        return state;
    }
}