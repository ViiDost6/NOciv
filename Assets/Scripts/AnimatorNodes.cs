using UnityEngine; // Necesario para Animator

[System.Serializable]
public class SetAnimatorBoolNode : Node
{
    [ShowInEditor]
    public string parameterName; // El nombre del parámetro en el Animator, ej: "isWalking"

    [ShowInEditor]
    public bool value; // El valor (true/false) que queremos ponerle

    private Animator animator; // Una caché para no buscarlo en cada frame

    public override NodeState Evaluate(GameObject agent)
    {
        // 1. Encontrar el Animator (solo la primera vez)
        if (animator == null)
        {
            animator = agent.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"SetAnimatorBoolNode: No se encontró Animator en {agent.name}.", agent);
                state = NodeState.Failure;
                return state;
            }
        }

        // 2. Establecer el valor
        animator.SetBool(parameterName, value);
        state = NodeState.Success;
        return state;
    }
}

public class SetAnimatorTrigger : Node
{
    [ShowInEditor]
    public string triggerName; // El nombre del trigger en el Animator, ej: "shoot"

    private Animator animator; // Una caché para no buscarlo en cada frame

    public override NodeState Evaluate(GameObject agent)
    {
        // 1. Encontrar el Animator (solo la primera vez)
        if (animator == null)
        {
            animator = agent.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"SetAnimatorBoolNode: No se encontró Animator en {agent.name}.", agent);
                state = NodeState.Failure;
                return state;
            }
        }
        // 2. Establecer el valor
        animator.SetTrigger(triggerName);
        state = NodeState.Success;
        return state;
    }
}