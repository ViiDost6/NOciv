using UnityEngine;
using System.Collections.Generic;

// --- ESTADO DE ATAQUE ---
public class AttackState : CommanderState
{
    public AttackState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        // Estrategia: "A la yugular"
        // Busamos torres enemigas y recursos, con prioridad alta
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.AddRange(ctx.structureManager.PlayerTowerPositions);
        
        // Si no hay torres (raro), vamos a por recursos
        if (targets.Count == 0) targets.AddRange(ctx.structureManager.ResourcePositions);

        // Ordenamos al mapa pintar deseos con ALTA prioridad (20f)
        map.SetStrategicGoals(targets, 20f);
        Debug.Log("Estrategia ATAQUE: Objetivo Torres Enemigas.");
    }

    public override CommanderState CheckTransitions()
    {
        // Si perdemos muchas unidades -> Defensa
        if (ctx.GetMyUnitCount() < ctx.GetEnemyUnitCount() * 0.6f)
            return new DefenseState(ctx);

        // Si no hay enemigos visibles -> Explorar
        if (ctx.GetEnemyUnitCount() == 0)
            return new ExploreState(ctx);

        return this; // Mantener estado
    }
}

// --- ESTADO DE DEFENSA ---
public class DefenseState : CommanderState
{
    public DefenseState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        // Estrategia: "Tortuga"
        // Protegemos nuestras propias torres y recursos cercanos
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.AddRange(ctx.structureManager.EnemyTowerPositions); // "EnemyTowers" son las mías (IA)
        
        // Prioridad MEDIA (15f), pero el comportamiento defensivo viene
        // porque las unidades ya estarán cerca de estos puntos
        map.SetStrategicGoals(targets, 15f);
        Debug.Log("Estrategia DEFENSA: Replegar a bases.");
    }

    public override CommanderState CheckTransitions()
    {
        // Si recuperamos superioridad numérica -> Ataque
        if (ctx.GetMyUnitCount() > ctx.GetEnemyUnitCount() * 1.2f)
            return new AttackState(ctx);

        return this;
    }
}

// --- ESTADO DE EXPLORACIÓN ---
public class ExploreState : CommanderState
{
    public ExploreState(CommanderAI2 ctx) : base(ctx) { }

    public override void UpdateStrategy()
    {
        // Estrategia: "Expansión Económica"
        List<Vector2Int> targets = new List<Vector2Int>();
        targets.AddRange(ctx.structureManager.ResourcePositions);

        map.SetStrategicGoals(targets, 10f); // Prioridad baja
        Debug.Log("Estrategia EXPLORAR: Capturar recursos.");
    }

    public override CommanderState CheckTransitions()
    {
        // En cuanto detectamos amenaza real -> Defensa o Ataque
        // if (ctx.GetEnemyUnitCount() > 0)
        // {
        //     // Decisión simple basada en números
        //     if (ctx.GetMyUnitCount() > ctx.GetEnemyUnitCount())
        //         return new AttackState(ctx);
        //     else
        //         return new DefenseState(ctx);
        // }

        return this;
    }
}