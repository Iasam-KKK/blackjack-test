using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Map
{
    public class Node
    {
        public readonly Vector2Int point;
        public readonly List<Vector2Int> incoming = new List<Vector2Int>();
        public readonly List<Vector2Int> outgoing = new List<Vector2Int>();
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly NodeType nodeType;
        public readonly string blueprintName;
        public readonly string nodeInstanceId; // Unique per node placement
        public Vector2 position;

        public Node(NodeType nodeType, string blueprintName, Vector2Int point, string nodeInstanceId = null)
        {
            this.nodeType = nodeType;
            this.blueprintName = blueprintName;
            this.point = point;
            this.nodeInstanceId = nodeInstanceId ?? $"{nodeType}_{blueprintName}_{point.x}_{point.y}_{System.Guid.NewGuid().ToString("N")[..8]}";
        }

        public void AddIncoming(Vector2Int p)
        {
            if (incoming.Any(element => element.Equals(p)))
                return;

            incoming.Add(p);
        }

        public void AddOutgoing(Vector2Int p)
        {
            if (outgoing.Any(element => element.Equals(p)))
                return;

            outgoing.Add(p);
        }

        public void RemoveIncoming(Vector2Int p)
        {
            incoming.RemoveAll(element => element.Equals(p));
        }

        public void RemoveOutgoing(Vector2Int p)
        {
            outgoing.RemoveAll(element => element.Equals(p));
        }

        public bool HasNoConnections()
        {
            return incoming.Count == 0 && outgoing.Count == 0;
        }
    }
}