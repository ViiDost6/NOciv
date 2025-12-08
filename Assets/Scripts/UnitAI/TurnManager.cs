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
    
    [Header("Estado del Juego")]
    public int turno = 1; // FIX: Variable reintroducida para MatchInitializer

    [Header("UI References")]
    public Button endTurnButton;
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI turnResourceText;

    [Header("References")]
    public CommanderAI2 commanderAI;
    public UnitManager unitManager;
    
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
        // Si no hay MatchInitializer, arrancamos normal (para pruebas).
        if (FindObjectOfType<MatchInitializer>() == null)
        {
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        IniciarTurno();
    }

    // --- FIX: Método público para iniciar la partida correctamente ---
    public void IniciarTurno()
    {
        // 1. Resetear variables
        playerResources = 0;
        aiResources = 0;
        turno = 1;
        
        // 2. Contar edificios iniciales
        RecalculateBaseCounts();

        // 3. Empezar turno jugador
        StartPlayerTurn();
    }

    private void RecalculateBaseCounts()
    {
        if (unitManager == null) return;

        unitManager.playerBaseCount = 0;
        unitManager.aiBaseCount = 0;
        playerResourceBuildings = 0;
        aiResourceBuildings = 0;

        Building[] allBuildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach(var b in allBuildings)
        {
            if (b.isBase)
            {
                if (b.hasBeenClaimed == 1) unitManager.playerBaseCount++;
                else if (b.hasBeenClaimed == 2) unitManager.aiBaseCount++;
            }
            else // Recursos
            {
                if (b.hasBeenClaimed == 1) playerResourceBuildings++;
                else if (b.hasBeenClaimed == 2) aiResourceBuildings++;
            }
        }
        
        Debug.Log($"[TurnManager] Recuento Inicial -> Player Bases: {unitManager.playerBaseCount} | AI Bases: {unitManager.aiBaseCount}");
    }

    public void OnEndTurnButtonPressed()
    {
        if (currentTurnState != TurnState.PlayerTurn) return;

        if(AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClip, 1.0f);

        if(unitManager != null)
        {
            unitManager.DestroyUI();
            if(unitManager.currentUnitSelected != null) 
            {
                unitManager.ToggleAttackRange(false);
                unitManager.currentUnitSelected.SetOutline(false);
            }
            unitManager.currentUnitSelected = null;
            unitManager.currentState = UnitManager.State.NoSelection;
        }

        StartCoroutine(ExecuteAITurnRoutine());
    }

    private void StartPlayerTurn()
    {
        currentTurnState = TurnState.PlayerTurn;

        if(AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerTurnStart, 1.0f);

        if(turnIndicatorText != null) turnIndicatorText.text = $"Turno Jugador ({turno})";
        
        // Ingresos
        playerResources += resourcePerTurn * (playerResourceBuildings + 2);
        if(turnResourceText != null) turnResourceText.text = $"{playerResources}";

        if(endTurnButton != null) endTurnButton.interactable = true;
        
        ResetUnitsActions(true);
        if(unitManager != null) unitManager.UpdateButtonVisual();
    }

    private IEnumerator ExecuteAITurnRoutine()
    {
        currentTurnState = TurnState.AIProcessing;
        if(turnIndicatorText != null) turnIndicatorText.text = "Turno IA";

        aiResources += resourcePerTurn * (aiResourceBuildings + 2);

        if(endTurnButton != null) endTurnButton.interactable = false;
        
        ResetUnitsActions(false);
        
        // 1. Fase Estratégica
        if(commanderAI != null) commanderAI.PrepareTurn(); 
        
        yield return new WaitForSeconds(0.5f);

        currentTurnState = TurnState.AIExecuting;

        List<Unit> aiUnits = GetAllUnits(false);
        
        // 2. Fase Táctica
        foreach (Unit unit in aiUnits)
        {
            if (unit == null || unit.health <= 0) continue;

            AIUnitController controller = unit.GetComponent<AIUnitController>();
            if (controller == null) continue;

            controller.ResetBehavior();

            bool treeFinished = false;
            int watchdog = 0;

            while (!treeFinished && watchdog < 1000)
            {
                NodeState result = controller.ExecuteTree();
                
                if (result == NodeState.Running)
                {
                    yield return null;
                }
                else if (result == NodeState.Success || result == NodeState.Failure)
                {
                    treeFinished = true;
                }
                
                if (!controller.IsBusy) watchdog++;
                else watchdog = 0; 
            }

            yield return new WaitForSeconds(0.2f);
        }

        turno++; // Incrementamos turno al finalizar la IA
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

    public void EndGame(bool playerWon)
    {
        currentTurnState = TurnState.AIProcessing;
        StopAllCoroutines();
        
        if (unitManager != null)
        {
            if (unitManager.currentUnitSelected != null) unitManager.currentUnitSelected.SetOutline(false);
            unitManager.DestroyUI();
        }

        if(endTurnButton != null) endTurnButton.interactable = false;
        if(AudioManager.Instance != null) AudioManager.Instance.musicSource.Stop();
        
        if (playerWon)
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.victory, 1.0f);
            if(turnIndicatorText != null) turnIndicatorText.text = "¡Victoria!";
        }
        else
        {
            if(AudioManager.Instance != null) AudioManager.Instance.PlaySFX(AudioManager.Instance.defeat, 1.0f);
            if(turnIndicatorText != null) turnIndicatorText.text = "¡Derrota!";
        }
    }
}