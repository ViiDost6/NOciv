using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    
    public enum TurnState { PlayerTurn, AIProcessing, AIExecuting }
    public TurnState currentTurnState;
    
    [Header("UI References")]
    public Button endTurnButton;
    public TextMeshProUGUI turnIndicatorText;

    [Header("References")]
    public CommanderAI2 commanderAI;
    public UnitManager unitManager;
    
    private int turnNumber = 1;
    public int resourcePerTurn = 25;

    public int playerResources = 0;
    public int aiResources = 0;
    public int playerResourceBuildings = 0;
    public int aiResourceBuildings = 0;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartPlayerTurn();
    }

    public void OnEndTurnButtonPressed()
    {
        if (currentTurnState == TurnState.PlayerTurn)
        {
            unitManager.DestroyUI();
            unitManager.ToggleAttackRange(false);
            unitManager.currentUnitSelected.SetOutline(false);
            unitManager.currentUnitSelected = null;
            unitManager.currentState = UnitManager.State.NoSelection;
            StartCoroutine(ExecuteAITurnRoutine());
        }
    }

    private void StartPlayerTurn()
    {
        currentTurnState = TurnState.PlayerTurn;
        turnIndicatorText.text = $"Turno {turnNumber}: JUGADOR";

        playerResources += resourcePerTurn * (playerResourceBuildings + 2);

        endTurnButton.interactable = true;
        ResetUnitsActions(true);
        unitManager.UpdateButtonVisual();
    }

    private IEnumerator ExecuteAITurnRoutine()
    {
        currentTurnState = TurnState.AIProcessing;
        turnIndicatorText.text = "Turno IA: Pensando...";

        aiResources += resourcePerTurn * (aiResourceBuildings + 2);

        endTurnButton.interactable = false;
        ResetUnitsActions(false);
        commanderAI.PrepareTurn(); 
        
        yield return new WaitForSeconds(0.5f);

        currentTurnState = TurnState.AIExecuting;
        turnIndicatorText.text = "Turno IA: Ejecutando...";

        List<Unit> aiUnits = GetAllUnits(false);
        bool anyUnitActed = true;
        int securityLoopBreak = 0;
        
        // Loop principal de acciones de la IA
        while (anyUnitActed && securityLoopBreak < 20) 
        {
            anyUnitActed = false;
            foreach (Unit unit in aiUnits)
            {
                if (unit == null) continue;

                AIUnitController controller = unit.GetComponent<AIUnitController>();
                
                // Solo actuamos si la unidad tiene movimientos y no está muerta
                if (controller != null && unit.movesLeftThisTurn > 0 && unit.health > 0)
                {
                    // Ejecutar Behavior Tree
                    NodeState result = controller.ExecuteTree();
                    
                    if (result == NodeState.Success)
                    {
                        anyUnitActed = true;
                        
                        // Esperar un frame para asegurar que IsBusy se ha actualizado
                        yield return null; 
                        
                        // Esperar mientras la unidad realiza su acción (Moverse/Atacar)
                        if (controller.IsBusy)
                        {
                            yield return new WaitUntil(() => !controller.IsBusy);
                        }
                        
                        // Pequeña pausa dramática entre unidades
                        yield return new WaitForSeconds(0.15f);
                    }
                }
            }
            securityLoopBreak++;
            // Pausa entre rondas de acciones para no congelar si el bucle es largo
            yield return null; 
        }

        turnNumber++;
        StartPlayerTurn();
    }

    private void ResetUnitsActions(bool isPlayer)
    {
        var units = GetAllUnits(isPlayer);
        foreach(var u in units)
        {
            if(u != null)
            {
                u.movesLeftThisTurn = u.movesTotal;
                u.hasAttackedThisTurn = false;
            }
        }
    }

    public List<Unit> GetAllUnits(bool playerUnits)
    {
        List<Unit> result = new List<Unit>();
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach(var u in allUnits)
        {
            if (u.isPlayerUnit == playerUnits) result.Add(u);
        }
        return result;
    }
}