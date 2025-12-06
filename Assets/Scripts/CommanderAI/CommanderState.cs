using UnityEngine;

public abstract class CommanderState
{
    protected CommanderAI2 ctx; // Referencia al contexto (CommanderAI)
    protected InfluenceMap2 map;

    public CommanderState(CommanderAI2 context)
    {
        this.ctx = context;
        this.map = context.GetInfluenceMap();
    }

    // Se ejecuta una vez al entrar al estado
    public virtual void Enter() 
    {
        Debug.Log($"[FSM] Entrando en estado: {this.GetType().Name}");
    }

    // Se ejecuta una vez al salir del estado
    public virtual void Exit() { }

    // Aquí va la lógica única de cada estado (Atacar, Defender, etc.)
    public abstract void UpdateStrategy();

    // Aquí decidimos si cambiamos de estado
    public abstract CommanderState CheckTransitions();
}