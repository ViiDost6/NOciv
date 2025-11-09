using UnityEngine;

// Este script contiene las "habilidades" del enemigo.
public class EnemyActions : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // --- ACCIÓN 1 ---
    // Esta es una función PÚBLICA que el BT puede llamar.
    public void PerformAttack()
    {
        if (animator != null)
        {
            Debug.Log("ACCIÓN: ¡Ejecutando ataque!");
            animator.SetTrigger("Attack");
        }
        else
        {
            Debug.LogWarning("ACCIÓN: ¡Quise atacar pero no tengo Animator!");
        }
    }

    // --- ACCIÓN 2 ---
    public void Celebrate()
    {
        if (animator != null)
        {
            Debug.Log("ACCIÓN: ¡Celebrando!");
            animator.SetTrigger("Celebrate");
        }else
        {
            Debug.LogWarning("ACCIÓN: ¡Quise celebrar pero no tengo Animator!"); 
        }
    }

    // --- ACCIÓN 3 ---
    public void PrintMessage(string message)
    {
        Debug.Log($"ACCIÓN: {message}");
    }
}