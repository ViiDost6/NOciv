using UnityEngine;
using System.Collections.Generic;

// --- CLASE BASE COMPLETA (Con Helpers requeridos) ---
public abstract class CommanderState
{
    protected CommanderAI2 ctx; 
    protected InfluenceMap2 map;

    public CommanderState(CommanderAI2 context)
    {
        this.ctx = context;
        this.map = context.GetInfluenceMap();
    }

    public virtual void Enter() 
    {
        Debug.Log($"[FSM] Entrando en estado: {this.GetType().Name}");
    }

    public virtual void Exit() { }

    public abstract void UpdateStrategy();
    public abstract CommanderState CheckTransitions();

    // --- HELPERS (Faltaban estos métodos) ---

    // Comprueba si quedan torres enemigas en pie (independientemente de si vemos unidades)
    protected bool AreEnemyBasesActive()
    {
        foreach (var pos in ctx.structureManager.PlayerTowerPositions)
        {
            TileData tile = ctx.structureManager.mapGenerator.GetTileAtPosition(pos);
            // Si la torre existe y NO es nuestra (es del Jugador 1), aún hay guerra.
            if (tile != null && tile.currentBuilding != null && tile.currentBuilding.hasBeenClaimed != 2)
                return true;
        }
        return false;
    }

    // Comprueba si queda algo que valga la pena explorar/capturar (Recursos neutrales o enemigos)
    protected bool AreThereCapturableResources()
    {
        foreach (var pos in ctx.structureManager.ResourcePositions)
        {
            TileData tile = ctx.structureManager.mapGenerator.GetTileAtPosition(pos);
            // Si no es nuestro (es 0 o 1), vale la pena ir.
            if (tile != null && tile.currentBuilding != null && tile.currentBuilding.hasBeenClaimed != 2)
                return true;
        }
        return false;
    }
}

// --- ESTADO DE ATAQUE ---
public class AttackState : CommanderState
{
    public AttackState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        List<Vector2Int> targets = map.GetWeakestTargets(ctx.structureManager.PlayerTowerPositions, 3.0f);
        
        if (targets.Count > 0)
        {
            map.SetStrategicGoals(targets, 25f, allyBias: 0.5f, enemyBias: 2.0f);
            Debug.Log("Estrategia ATAQUE: Quirúrgico.");
        }
        else
        {
            List<Vector2Int> allTargets = new List<Vector2Int>();
            allTargets.AddRange(ctx.structureManager.PlayerTowerPositions);
            if (allTargets.Count == 0) allTargets.AddRange(ctx.structureManager.ResourcePositions);
            map.SetStrategicGoals(allTargets, 18f, allyBias: 0.8f, enemyBias: 1.5f);
            Debug.Log("Estrategia ATAQUE: General.");
        }
    }

    public override CommanderState CheckTransitions()
    {
        if (ctx.Vulnerability > 0.1f) 
        {
            Debug.Log("Transition -> Defense (Amenaza detectada)");
            return new DefenseState(ctx);
        }

        if (ctx.Tension > 0.6f && ctx.EnemyExposure < 0.3f && ctx.GetMyUnitCount() < ctx.GetEnemyUnitCount())
            return new DefenseState(ctx);

        if (ctx.GetEnemyUnitCount() == 0 && !AreEnemyBasesActive())
        {
            if (AreThereCapturableResources()) return new ExploreState(ctx);
        }

        return this;
    }
}

// --- ESTADO DE DEFENSA ---
public class DefenseState : CommanderState
{
    public DefenseState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.AddRange(ctx.structureManager.EnemyTowerPositions);
        if (ctx.EconomicSafety < 0.4f) targets.AddRange(ctx.structureManager.ResourcePositions);

        map.SetStrategicGoals(targets, 25f, allyBias: 3.0f, enemyBias: 0.2f);
        Debug.Log("Estrategia DEFENSA: Muro de Escudos.");
    }

    public override CommanderState CheckTransitions()
    {
        bool safeBase = ctx.Vulnerability < 0.05f; 

        if (ctx.EnemyExposure > 0.8f && ctx.Vulnerability < 0.2f) 
            return new AttackState(ctx);

        if (safeBase && ctx.GetMyUnitCount() > ctx.GetEnemyUnitCount())
            return new AttackState(ctx);

        if (ctx.GetEnemyUnitCount() == 0 && safeBase && AreThereCapturableResources())
            return new ExploreState(ctx);

        return this;
    }
}

// --- ESTADO DE EXPLORACIÓN ---
public class ExploreState : CommanderState
{
    public ExploreState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.AddRange(ctx.structureManager.ResourcePositions);
        map.SetStrategicGoals(targets, 12f, allyBias: 1.0f, enemyBias: 1.0f); 
        Debug.Log("Estrategia EXPLORAR: Economía.");
    }

    public override CommanderState CheckTransitions()
    {
        if (!AreThereCapturableResources()) return new AttackState(ctx);
        if (ctx.EnemyExposure > 0.8f) return new AttackState(ctx);
        if (ctx.Tension > 0.2f)
        {
            if (ctx.GetMyUnitCount() >= ctx.GetEnemyUnitCount()) return new AttackState(ctx);
            else return new DefenseState(ctx);
        }
        
        if (ctx.Vulnerability > 0.1f) return new DefenseState(ctx);

        return this;
    }
}