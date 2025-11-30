using UnityEngine;

public class AIActionsHelper : MonoBehaviour
{
    public static AIActionsHelper Instance;

    private void Awake()
    {
        Instance = this;
    }

    // --- LÓGICA PIEDRA-PAPEL-TIJERA (RPS) ---
    // 1 = Ventaja (Soy su Counter), -1 = Desventaja (Es mi Counter), 0 = Neutral
    public int GetRPSMatchup(Unit me, Unit enemy)
    {
        if (me == null || enemy == null) return 0;

        // Regla: Infantería > Pesada > Artillería > Infantería
        switch (me.unitType)
        {
            case Unit.UnitType.Infantry:
                if (enemy.unitType == Unit.UnitType.HeavyInfantry) return -1;  // Pierde
                if (enemy.unitType == Unit.UnitType.Artillery) return 1;     // Gana
                break;

            case Unit.UnitType.HeavyInfantry:
                if (enemy.unitType == Unit.UnitType.Artillery) return -1;      // Pierde
                if (enemy.unitType == Unit.UnitType.Infantry) return 1;      // Gana
                break;

            case Unit.UnitType.Artillery:
                if (enemy.unitType == Unit.UnitType.Infantry) return -1;       // Pierde
                if (enemy.unitType == Unit.UnitType.HeavyInfantry) return 1; // Gana
                break;
        }
        return 0; // Empate o mismo tipo
    }

    public void MoveUnit(Unit unit, TileData targetTile)
    {
        if (unit == null || targetTile == null) return;
        
        // Movimiento visual y lógico
        unit.transform.position = new Vector3(targetTile.transform.position.x, targetTile.transform.position.y, -1);
        
        if (unit.currentTile != null) 
            unit.currentTile.hasUnit = false;
        
        unit.currentTile = targetTile;
        targetTile.hasUnit = true;
        unit.movesLeftThisTurn--;
    }

    public void AttackUnit(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null) return;
        
        // Bonus de daño táctico si tengo ventaja de tipo
        int rps = GetRPSMatchup(attacker, defender);
        int damageBonus = (rps == 1) ? 2 : 0; 
        
        Debug.Log($"IA COMBATE: {attacker.name} vs {defender.name} | RPS: {rps} | Bonus: {damageBonus}");

        int totalDamage = attacker.damage + damageBonus;

        // Cálculo de armadura
        if(defender.hasArmor && !attacker.hasPiercing) 
        {
            totalDamage = Mathf.Max(0, totalDamage - 1);
        }

        defender.health -= totalDamage;

        if(defender.health <= 0) 
        {
            defender.Death();
        }
        
        // Atacar suele consumir el turno o todos los movimientos
        attacker.movesLeftThisTurn = 0; 
    }
}