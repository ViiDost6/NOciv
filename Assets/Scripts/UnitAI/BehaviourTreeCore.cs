using System.Collections.Generic;
using UnityEngine;

namespace AI.BehaviourTree
{
    public enum NodeState { RUNNING, SUCCESS, FAILURE }

    public abstract class Node
    {
        protected NodeState state;
        public Node parent;
        protected List<Node> children = new List<Node>();
        protected Dictionary<string, object> dataContext; // Blackboard local

        public Node() { parent = null; }
        public Node(List<Node> children) { foreach (var c in children) Attach(c); }

        public void Attach(Node node)
        {
            node.parent = this;
            children.Add(node);
        }

        public virtual NodeState Evaluate() => NodeState.FAILURE;

        public void SetData(string key, object value)
        {
            if (dataContext == null) dataContext = new Dictionary<string, object>();
            dataContext[key] = value;
        }

        public object GetData(string key)
        {
            object val = null;
            if (dataContext != null && dataContext.TryGetValue(key, out val)) return val;
            Node node = parent;
            while (node != null)
            {
                val = node.GetData(key);
                if (val != null) return val;
                node = node.parent;
            }
            return null;
        }

        public bool ClearData(string key)
        {
            if (dataContext != null && dataContext.ContainsKey(key))
            {
                dataContext.Remove(key);
                return true;
            }
            Node node = parent;
            while (node != null)
            {
                if (node.ClearData(key)) return true;
                node = node.parent;
            }
            return false;
        }
    }

    // --- NODOS COMPUESTOS ---

    public class Selector : Node
    {
        public Selector() : base() { }
        public Selector(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE: continue;
                    case NodeState.SUCCESS: return state = NodeState.SUCCESS;
                    case NodeState.RUNNING: return state = NodeState.RUNNING;
                }
            }
            return state = NodeState.FAILURE;
        }
    }

    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE: return state = NodeState.FAILURE;
                    case NodeState.SUCCESS: continue;
                    case NodeState.RUNNING: anyChildRunning = true; continue;
                }
            }
            return state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
        }
    }

    // --- DECORADORES ---

    public class Inverter : Node
    {
        private Node child;
        public Inverter(Node node) { child = node; }
        public override NodeState Evaluate()
        {
            switch (child.Evaluate())
            {
                case NodeState.FAILURE: return state = NodeState.SUCCESS;
                case NodeState.SUCCESS: return state = NodeState.FAILURE;
                case NodeState.RUNNING: return state = NodeState.RUNNING;
            }
            return state = NodeState.FAILURE;
        }
    }
}