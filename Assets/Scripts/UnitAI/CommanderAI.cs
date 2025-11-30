using UnityEngine;
using System.Collections.Generic;

// Estados posibles del ejército
public enum GlobalOrder
{
    ADVANCE,        // Presión estándar
    ALL_OUT_ATTACK, // Agresividad máxima, ignora riesgos leves
    DEFEND_BASE,    // Retroceder a puntos seguros
    RETREAT         // Huida prioritaria
}

public class CommanderAI : MonoBehaviour
{
    public static CommanderAI Instance;
    public InfluenceMap influenceMap;
    public bool isPlayerCommander = false; // False = IA enemiga

    [Header("Debug")]
    public GlobalOrder currentGlobalOrder;

    private void Awake()
    {
        Instance = this;
    }

    public void ExecuteCommanderTurn()
    {
        // 1. ANALIZAR EL CAMPO Y EMITIR UNA ÚNICA ORDEN GLOBAL
        currentGlobalOrder = AnalyzeBattlefield();
        Debug.Log($"<color=cyan>COMANDANTE:</color> Orden Global emitida: <b>{currentGlobalOrder}</b>");

        // 2. SELECCIONAR UNIDADES
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        List<Unit> myUnits = new List<Unit>();
        foreach(var u in allUnits)
        {
            if (u.isPlayerUnit == isPlayerCommander) myUnits.Add(u);
        }

        // 3. EJECUCIÓN SECUENCIAL
        foreach (Unit unit in myUnits)
        {
            Debug.Log(unit.name);
            AIBlackboard blackboard = unit.GetComponent<AIBlackboard>();
            BehaviourTreeRunner runner = unit.GetComponent<BehaviourTreeRunner>();
            
            if (blackboard == null || runner == null) continue;

            // A. Comunicar la orden global a la unidad
            blackboard.SetData("GlobalOrder", currentGlobalOrder);
            
            // B. Preparar la unidad
            unit.movesLeftThisTurn = unit.movesTotal;

            // C. Bucle de ejecución (Mientras le queden movimientos y tenga éxito)
            int safetyBreaker = 25; 
            bool turnActive = true;

            while (turnActive && unit.movesLeftThisTurn > 0 && safetyBreaker > 0)
            {
                // La unidad interpreta la orden global + su situación local (RPS) en cada paso
                NodeState result = runner.RunTree();
                
                if (result == NodeState.Failure)
                {
                    // La unidad decidió no hacer nada más (ej. está en buena posición y no quiere arriesgar)
                    turnActive = false;
                }
                else if (result == NodeState.Success)
                {
                    // Realizó una acción (moverse 1 casilla o atacar)
                    // Seguimos el bucle para gastar el resto de movimientos
                    safetyBreaker--;
                }
            }
        }
    }

    private GlobalOrder AnalyzeBattlefield()
    {
        // Contar fuerzas
        int myUnits = 0;
        int enemyUnits = 0;
        float myTotalHealth = 0;

        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach(var u in allUnits)
        {
            if(u.isPlayerUnit == isPlayerCommander)
            {
                myUnits++;
                myTotalHealth += u.health;
            }
            else
            {
                enemyUnits++;
            }
        }

        // Lógica de decisión
        if (myUnits == 0) return GlobalOrder.RETREAT; // No hay nadie, da igual

        // 1. Si somos muchos menos (menos de la mitad) -> Defender
        if (myUnits < enemyUnits * 0.5f) return GlobalOrder.DEFEND_BASE;

        // 2. Si estamos muy heridos en promedio -> Retirada
        float avgHealth = myTotalHealth / myUnits;
        if (avgHealth < 2.5f) return GlobalOrder.RETREAT;

        // 3. Si tenemos ventaja numérica -> Ataque total
        if (myUnits > enemyUnits * 1.2f) return GlobalOrder.ALL_OUT_ATTACK;

        // 4. Por defecto -> Avance
        return GlobalOrder.ADVANCE;
    }
}