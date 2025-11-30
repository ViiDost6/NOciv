using UnityEngine;
using System.Collections.Generic;

// Agrega este componente a tus Unidades junto con el BehaviourTreeRunner.
// Sirve de memoria compartida para los nodos.
public class AIBlackboard : MonoBehaviour
{
    private Dictionary<string, object> data = new Dictionary<string, object>();

    public void SetData(string key, object value)
    {
        data[key] = value;
    }

    public object GetData(string key)
    {
        if (data.TryGetValue(key, out object value))
        {
            return value;
        }
        return null;
    }

    public T GetData<T>(string key)
    {
        object val = GetData(key);
        if (val == null) return default(T);
        return (T)val;
    }

    public bool HasData(string key)
    {
        return data.ContainsKey(key);
    }

    public void ClearData(string key)
    {
        if (data.ContainsKey(key)) data.Remove(key);
    }
}