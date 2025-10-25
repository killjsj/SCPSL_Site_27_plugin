using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MapGeneration;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Next_generationSite_27.UnionP
{
    public class RoomGraph
    {
        public static RoomGraph Instance;
        public Dictionary<Room, RoomNode> Nodes { get; private set; } = new Dictionary<Room, RoomNode>();
        private bool building = false;
        public bool Built => Nodes.Count > 0 && !building;

        public static InternalNavigator InternalNav = new InternalNavigator();

        public RoomGraph()
        {
            Instance = this;
            BuildAsync();
        }

        // 异步构建图（防止卡顿）
        public void BuildAsync()
        {
            if (building) return;
            building = true;
            Nodes.Clear();
            Timing.RunCoroutine(_BuildRoutine());
        }

        private IEnumerator<float> _BuildRoutine()
        {
            Log.Info("[RoomGraph] 开始构建房间图...");
            var connectedPairs = new HashSet<RoomPair>();

            foreach (var room in Room.List)
            {
                if (room == null) continue;
                Nodes[room] = new RoomNode(room);
                yield return Timing.WaitForOneFrame;
            }

            // --- 门连接 ---
            foreach (var door in Door.List.ToList())
            {
                if (door == null) continue;

                var rooms = door.Rooms?.ToList();
                if (rooms == null || rooms.Count < 2)
                {
                    // 单向门（电梯等）跳过
                    continue;
                }

                var rA = rooms.ElementAtOrDefault(0);
                var rB = rooms.ElementAtOrDefault(1);
                if (rA == null || rB == null) continue;

                if (!Nodes.ContainsKey(rA) || !Nodes.ContainsKey(rB)) continue;

                var edgeAB = new RoomEdge(Nodes[rA], Nodes[rB], door);
                var edgeBA = new RoomEdge(Nodes[rB], Nodes[rA], door);
                Nodes[rA].Edges.Add(edgeAB);
                Nodes[rB].Edges.Add(edgeBA);
                connectedPairs.Add(new RoomPair(rA.Identifier, rB.Identifier));

                yield return Timing.WaitForSeconds(0.01f);
            }

            // --- 邻近房间连接 ---
            foreach (var room in Room.List)
            {
                if (room == null || !Nodes.TryGetValue(room, out var nodeA)) continue;

                foreach (var neighbor in room.NearestRooms)
                {
                    if (neighbor == null || neighbor == room) continue;
                    if (!Nodes.TryGetValue(neighbor, out var nodeB)) continue;
                    var pair = new RoomPair(room.Identifier, neighbor.Identifier);
                    if (connectedPairs.Contains(pair)) continue;

                    var mid = (room.Position + neighbor.Position) * 0.5f;
                    var edgeA = new RoomEdge(nodeA, nodeB, null, RoomEdgeType.Transition, mid);
                    var edgeB = new RoomEdge(nodeB, nodeA, null, RoomEdgeType.Transition, mid);
                    nodeA.Edges.Add(edgeA);
                    nodeB.Edges.Add(edgeB);
                    connectedPairs.Add(pair);
                }

                yield return Timing.WaitForOneFrame;
            }

            building = false;
            Log.Info($"[RoomGraph] 构建完成: {Nodes.Count} 个房间, {Nodes.Sum(n => n.Value.Edges.Count)} 条边。");
        }

        // --- 跨房间寻路（A*） ---
        public List<Room> GetRoomPath(Room start, Room end)
        {
            if (start == null || end == null || !Built)
                return null;

            var open = new PriorityQueue<RoomNode, float>();
            var came = new Dictionary<RoomNode, RoomEdge>();
            var g = new Dictionary<RoomNode, float>();
            var f = new Dictionary<RoomNode, float>();

            foreach (var n in Nodes.Values)
            {
                g[n] = float.MaxValue;
                f[n] = float.MaxValue;
            }

            var s = Nodes[start];
            var e = Nodes[end];
            g[s] = 0;
            f[s] = Vector3.Distance(s.Position, e.Position);
            open.Enqueue(s, f[s]);

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                if (cur == e)
                    return ReconstructRoomPath(came, cur, s);

                foreach (var edge in cur.Edges)
                {
                    if (!IsEdgePassable(edge)) continue;
                    var nb = edge.To;
                    var cost = g[cur] + Vector3.Distance(cur.Position, edge.ConnectionPoint);
                    if (cost < g[nb])
                    {
                        came[nb] = edge;
                        g[nb] = cost;
                        f[nb] = cost + Vector3.Distance(nb.Position, e.Position);
                        open.EnqueueOrUpdate(nb, f[nb]);
                    }
                }
            }

            return null;
        }

        private List<Room> ReconstructRoomPath(Dictionary<RoomNode, RoomEdge> came, RoomNode end, RoomNode start)
        {
            var path = new Stack<Room>();
            var current = end;
            path.Push(current.Room);

            RoomEdge edge;
            while (came.TryGetValue(current, out edge))
            {
                current = edge.From;
                path.Push(current.Room);
                if (current == start) break;
            }

            return path.ToList();
        }

        private static bool IsEdgePassable(RoomEdge edge)
        {
            if (edge.Type == RoomEdgeType.Transition)
                return true;
            if (edge.Door == null) return false;
            return edge.Door.IsOpen || edge.Door.IsCheckpoint || edge.Door.IsElevator;
        }

        // ================== 内部导航器 ===================
        public class InternalNavigator
        {
            public List<Vector3> FindPath(Vector3 start, Room startRoom, Vector3 end, Room endRoom)
            {
                if (startRoom == null)
                    startRoom = Room.Get(start);
                if (endRoom == null)
                    endRoom = Room.Get(end);

                // 1️⃣ 房间相同 → 直接用内部导航
                if (startRoom == endRoom)
                    return LocalPathInRoom(start, end, startRoom);

                // 2️⃣ 跨房间 → 用 RoomGraph 的房间路径
                var rooms = RoomGraph.Instance.GetRoomPath(startRoom, endRoom);
                if (rooms == null || rooms.Count == 0)
                    return new List<Vector3> { start, end };

                var fullPath = new List<Vector3> { start };
                for (int i = 0; i < rooms.Count - 1; i++)
                {
                    var a = rooms[i];
                    var b = rooms[i + 1];
                    var doorEdge = a.Doors.FirstOrDefault(d => d.Rooms.Contains(b));
                    Vector3 connect = doorEdge?.Position ?? (a.Position + b.Position) * 0.5f;

                    var local = LocalPathInRoom(fullPath.Last(), connect, a);
                    if (local != null)
                        fullPath.AddRange(local.Skip(1));
                }

                var lastPath = LocalPathInRoom(fullPath.Last(), end, endRoom);
                if (lastPath != null)
                    fullPath.AddRange(lastPath.Skip(1));

                return fullPath;
            }

            public List<Vector3> LocalPathInRoom(Vector3 start, Vector3 end, Room room)
            {
                var result = new List<Vector3> { start };

                if (IsDirectPathClear(start, end))
                {
                    result.Add(end);
                    return result;
                }

                var points = new List<Vector3>(GetWalkablePoints(room));
                if (points.Count == 0)
                {
                    result.Add(end);
                    return result;
                }

                // 建图
                var graph = new Dictionary<Vector3, List<Vector3>>();
                foreach (var p in points)
                    graph[p] = new List<Vector3>();

                for (int i = 0; i < points.Count; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        if (Vector3.Distance(points[i], points[j]) < 8f &&
                            IsDirectPathClear(points[i], points[j]))
                        {
                            graph[points[i]].Add(points[j]);
                            graph[points[j]].Add(points[i]);
                        }
                    }
                }

                // 接入 start/end
                graph[start] = new List<Vector3>();
                graph[end] = new List<Vector3>();
                foreach (var p in points)
                {
                    if (Vector3.Distance(start, p) < 6f && IsDirectPathClear(start, p))
                        graph[start].Add(p);
                    if (Vector3.Distance(end, p) < 6f && IsDirectPathClear(end, p))
                        graph[p].Add(end);
                }

                // A* 搜索
                var came = new Dictionary<Vector3, Vector3>();
                var g = new Dictionary<Vector3, float>();
                var f = new Dictionary<Vector3, float>();
                var open = new List<Vector3> { start };

                foreach (var p in graph.Keys)
                {
                    g[p] = float.MaxValue;
                    f[p] = float.MaxValue;
                }

                g[start] = 0;
                f[start] = Vector3.Distance(start, end);

                while (open.Count > 0)
                {
                    var current = open.OrderBy(p => f[p]).First();
                    open.Remove(current);

                    if (Vector3.Distance(current, end) < 0.5f)
                    {
                        var path = new List<Vector3>();
                        var c = current;
                        path.Add(end);
                        while (came.ContainsKey(c))
                        {
                            path.Add(c);
                            c = came[c];
                        }
                        path.Add(start);
                        path.Reverse();
                        return path;
                    }

                    foreach (var neighbor in graph[current])
                    {
                        float tentative = g[current] + Vector3.Distance(current, neighbor);
                        if (tentative < g[neighbor])
                        {
                            came[neighbor] = current;
                            g[neighbor] = tentative;
                            f[neighbor] = tentative + Vector3.Distance(neighbor, end);
                            if (!open.Contains(neighbor))
                                open.Add(neighbor);
                        }
                    }
                }

                result.Add(end);
                return result;
            }

            // 获取可行走点
            private static List<Vector3> GetWalkablePoints(Room room)
            {
                var list = new List<Vector3>();
                if (room?.GameObject == null) return list;

                foreach (var c in room.GameObject.GetComponentsInChildren<Collider>(true))
                {
                    var box = c as BoxCollider;
                    if (box != null)
                    {
                        var corners = GetBoxCorners(box);
                        foreach (var p in corners)
                            if (IsPositionStandable(p)) list.Add(p);
                    }
                }

                return list;
            }

            private static Vector3[] GetBoxCorners(BoxCollider box)
            {
                Vector3 center = box.center;
                Vector3 ext = box.size * 0.5f;
                var corners = new Vector3[8];
                int i = 0;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                            corners[i++] = box.transform.TransformPoint(center + new Vector3(x * ext.x, y * ext.y, z * ext.z));
                return corners;
            }

            public static bool IsDirectPathClear(Vector3 from, Vector3 to)
            {
                return !Physics.Linecast(from, to);
            }

            private static bool IsPositionStandable(Vector3 pos)
            {
                return Physics.CheckSphere(pos + Vector3.down * 0.1f, 0.3f, -1);
            }

            public List<Room> GetPathRooms(Room start, Room end)
            {
                return RoomGraph.Instance.GetRoomPath(start, end);
            }

            public List<Vector3> FindPath(Vector3 start, Vector3 end, Room endRoom)
            {
                return LocalPathInRoom(start, end, endRoom);
            }
        }
    }

    // --- 基础类型 ---
    public class RoomNode
    {
        public Room Room { get; private set; }
        public Vector3 Position { get { return Room.Position; } }
        public HashSet<RoomEdge> Edges { get; private set; }
        public RoomNode(Room r)
        {
            Room = r;
            Edges = new HashSet<RoomEdge>();
        }
    }

    public enum RoomEdgeType { Door, Transition }

    public class RoomEdge
    {
        public RoomNode From { get; private set; }
        public RoomNode To { get; private set; }
        public Door Door { get; private set; }
        public RoomEdgeType Type { get; private set; }
        public Vector3 ConnectionPoint { get; private set; }

        public RoomEdge(RoomNode from, RoomNode to, Door door, RoomEdgeType type = RoomEdgeType.Door, Vector3? point = null)
        {
            From = from;
            To = to;
            Door = door;
            Type = type;
            ConnectionPoint = point ?? (door != null ? door.Position : (from.Position + to.Position) * 0.5f);
        }
    }

    public struct RoomPair : IEquatable<RoomPair>
    {
        public RoomIdentifier A;
        public RoomIdentifier B;
        public RoomPair(RoomIdentifier a, RoomIdentifier b)
        {
            if (a.MainCoords.x < b.MainCoords.x || (a.MainCoords.x == b.MainCoords.x && a.MainCoords.z < b.MainCoords.z))
            {
                A = a; B = b;
            }
            else
            {
                A = b; B = a;
            }
        }
        public bool Equals(RoomPair o) { return A == o.A && B == o.B; }
        public override int GetHashCode() { return A.GetHashCode() * 31 ^ B.GetHashCode(); }
    }

    public class PriorityQueue<T, P> where P : IComparable<P>
    {
        private readonly List<KeyValuePair<T, P>> _heap = new List<KeyValuePair<T, P>>();
        public int Count { get { return _heap.Count; } }

        public void Enqueue(T item, P prio)
        {
            _heap.Add(new KeyValuePair<T, P>(item, prio));
            HeapifyUp(_heap.Count - 1);
        }

        public void EnqueueOrUpdate(T item, P prio)
        {
            for (int i = 0; i < _heap.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_heap[i].Key, item))
                {
                    _heap[i] = new KeyValuePair<T, P>(item, prio);
                    HeapifyUp(i);
                    HeapifyDown(i);
                    return;
                }
            }
            Enqueue(item, prio);
        }

        public T Dequeue()
        {
            T top = _heap[0].Key;
            _heap[0] = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            HeapifyDown(0);
            return top;
        }

        private void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_heap[i].Value.CompareTo(_heap[p].Value) >= 0) break;
                var tmp = _heap[i];
                _heap[i] = _heap[p];
                _heap[p] = tmp;
                i = p;
            }
        }

        private void HeapifyDown(int i)
        {
            while (true)
            {
                int l = 2 * i + 1, r = 2 * i + 2, s = i;
                if (l < _heap.Count && _heap[l].Value.CompareTo(_heap[s].Value) < 0) s = l;
                if (r < _heap.Count && _heap[r].Value.CompareTo(_heap[s].Value) < 0) s = r;
                if (s == i) break;
                var tmp = _heap[i];
                _heap[i] = _heap[s];
                _heap[s] = tmp;
                i = s;
            }
        }
    }

    // --- 向后兼容旧接口 ---
    public static class RoomGraphExtensions
    {
        public static List<Vector3> LocalPathInRoom(Vector3 start, Vector3 end, Room room)
            => RoomGraph.InternalNav.LocalPathInRoom(start, end, room);

        public static List<Vector3> FindPath(Vector3 start, Vector3 end, Room endRoom)
            => RoomGraph.InternalNav.FindPath(start, end, endRoom);

        public static List<Room> GetPathRooms(Room startRoom, Room endRoom)
            => RoomGraph.InternalNav.GetPathRooms(startRoom, endRoom);

        public static bool IsDirectPathClear(Vector3 from, Vector3 to)
            => Physics.Linecast(from, to);
    }
}
