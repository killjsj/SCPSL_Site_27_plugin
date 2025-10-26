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
    // 完整优化版 RoomGraph，可直接替换使用
    public class RoomGraph
    {
        public static RoomGraph Instance;
        public Dictionary<Room, RoomNode> Nodes { get; private set; } = new Dictionary<Room, RoomNode>();
        private volatile bool building = false;
        public bool Built => Nodes.Count > 0 && !building;

        public static InternalNavigator InternalNav = new InternalNavigator();

        public RoomGraph()
        {
            Instance = this;
            BuildAsync();
        }

        // ---------------- 异步构建（批次 + 预分配 + 减少 GC） ----------------
        public void BuildAsync()
        {
            if (building) return;
            building = true;
            Timing.RunCoroutine(_BuildRoutine());
        }

        private IEnumerator<float> _BuildRoutine()
        {
            Log.Info("[RoomGraph] 开始构建房间图...");
            Nodes.Clear();

            var rooms = Room.List.Where(r => r != null).ToArray();
            Nodes = new Dictionary<Room, RoomNode>(rooms.Length);

            int batch = 0;
            // 建立节点
            foreach (var room in rooms)
            {
                Nodes[room] = new RoomNode(room);
                if (++batch % 16 == 0) yield return Timing.WaitForOneFrame;
            }

            // 门连边
            var connectedPairs = new HashSet<RoomPair>(rooms.Length * 2);
            batch = 0;
            foreach (var door in Door.List)
            {
                if (door == null || door.Rooms == null) continue;

                var arr = door.Rooms.ToArray();
                if (arr.Length < 2) continue; // 单向门
                var rA = arr[0];
                var rB = arr[1];
                if (rA == null || rB == null) continue;
                if (!Nodes.TryGetValue(rA, out var aNode) || !Nodes.TryGetValue(rB, out var bNode)) continue;

                var edgeAB = new RoomEdge(aNode, bNode, door);
                var edgeBA = new RoomEdge(bNode, aNode, door);
                aNode.Edges.Add(edgeAB);
                bNode.Edges.Add(edgeBA);
                connectedPairs.Add(new RoomPair(rA.Identifier, rB.Identifier));

                if (++batch % 32 == 0) yield return Timing.WaitForOneFrame;
            }

            // 邻近房间连边（跳过已连接对）
            batch = 0;
            foreach (var room in rooms)
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
                if (++batch % 16 == 0) yield return Timing.WaitForOneFrame;
            }

            building = false;
            Log.Info($"[RoomGraph] 构建完成: {Nodes.Count} 个房间, {Nodes.Sum(n => n.Value.Edges.Count)} 条边。");
            yield break;
        }

        // ---------------- 跨房间 A*（使用快速优先队列 + lazy update） ----------------
        public List<Room> GetRoomPath(Room start, Room end)
        {
            if (start == null || end == null || !Built) return null;
            if (start == end) return new List<Room> { start };

            var s = Nodes[start];
            var e = Nodes[end];

            var open = new FastPriorityQueue<RoomNode>();
            var came = new Dictionary<RoomNode, RoomEdge>(Nodes.Count);
            var g = new Dictionary<RoomNode, float>(Nodes.Count);
            var visited = new HashSet<RoomNode>();

            foreach (var n in Nodes.Values) g[n] = float.MaxValue;

            g[s] = 0f;
            open.Enqueue(s, Vector3.Distance(s.Position, e.Position));

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                if (visited.Contains(cur)) continue; // lazy-dequeued duplicate
                visited.Add(cur);

                if (cur == e) return ReconstructRoomPath(came, cur, s);

                foreach (var edge in cur.Edges)
                {
                    if (!IsEdgePassable(edge)) continue;
                    var nb = edge.To;
                    var cost = g[cur] + Vector3.Distance(cur.Position, edge.ConnectionPoint);
                    if (cost + 0.0001f < g[nb])
                    {
                        came[nb] = edge;
                        g[nb] = cost;
                        var f = cost + Vector3.Distance(nb.Position, e.Position);
                        open.Enqueue(nb, f); // 允许重复进入堆，使用 visited 过滤已处理
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
            while (came.TryGetValue(current, out var edge))
            {
                current = edge.From;
                path.Push(current.Room);
                if (current == start) break;
            }
            return path.ToList();
        }

        private static bool IsEdgePassable(RoomEdge edge)
        {
            if (edge.Type == RoomEdgeType.Transition) return true;
            if (edge.Door == null) return false;
            return edge.Door.IsOpen || edge.Door.IsCheckpoint || edge.Door.IsElevator;
        }

        // ================== 内部导航器（局部寻路） ===================
        public class InternalNavigator
        {
            private const float NodeConnectDistance = 8f;
            private const float StartEndConnectDistance = 6f;
            private const float GridSize = 8f;

            // 帧内缓存（避免重复 Physics 调用）
            [ThreadStatic] private static Dictionary<(Vector3, Vector3), bool> _rayCache;
            private static Dictionary<Vector2Int, List<Vector3>> _gridCache = new Dictionary<Vector2Int, List<Vector3>>();

            public List<Vector3> FindPath(Vector3 start, Room startRoom, Vector3 end, Room endRoom)
            {
                if (startRoom == null) startRoom = Room.Get(start);
                if (endRoom == null) endRoom = Room.Get(end);

                if (startRoom == endRoom)
                    return LocalPathInRoom(start, end, startRoom);

                var rooms = RoomGraph.Instance?.GetRoomPath(startRoom, endRoom);
                if (rooms == null || rooms.Count == 0) return new List<Vector3> { start, end };

                var fullPath = new List<Vector3> { start };
                for (int i = 0; i < rooms.Count - 1; i++)
                {
                    var a = rooms[i];
                    var b = rooms[i + 1];
                    var doorEdge = a.Doors.FirstOrDefault(d => d.Rooms.Contains(b));
                    Vector3 connect = doorEdge?.Position ?? (a.Position + b.Position) * 0.5f;

                    var local = LocalPathInRoom(fullPath.Last(), connect, a);
                    if (local != null && local.Count > 0)
                        fullPath.AddRange(local.Skip(1));
                    else
                        fullPath.Add(connect);
                }

                var lastPath = LocalPathInRoom(fullPath.Last(), end, endRoom);
                if (lastPath != null && lastPath.Count > 0)
                    fullPath.AddRange(lastPath.Skip(1));
                else
                    fullPath.Add(end);

                ClearRayCache();
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

                // 空间分格缓存
                BuildGridCache(points);

                // 图结构
                var graph = new Dictionary<Vector3, List<Vector3>>(points.Count + 2);
                foreach (var p in points) graph[p] = new List<Vector3>();

                // 用网格邻域替代全 O(n^2) 检查
                foreach (var p in points)
                {
                    foreach (var n in GetNeighborsFromGrid(p))
                    {
                        if (p == n) continue;
                        if (Vector3.Distance(p, n) < NodeConnectDistance && IsDirectPathClear(p, n))
                        {
                            graph[p].Add(n);
                        }
                    }
                }

                // 接入 start/end
                graph[start] = new List<Vector3>();
                graph[end] = new List<Vector3>();
                foreach (var p in points)
                {
                    if (Vector3.Distance(start, p) < StartEndConnectDistance && IsDirectPathClear(start, p)) graph[start].Add(p);
                    if (Vector3.Distance(end, p) < StartEndConnectDistance && IsDirectPathClear(end, p)) graph[p].Add(end);
                }

                // A* 搜索（使用字典 + List open）
                var came = new Dictionary<Vector3, Vector3>();
                var g = new Dictionary<Vector3, float>();
                var f = new Dictionary<Vector3, float>();
                var open = new List<Vector3> { start };

                foreach (var p in graph.Keys) { g[p] = float.MaxValue; f[p] = float.MaxValue; }
                g[start] = 0f; f[start] = Vector3.Distance(start, end);

                while (open.Count > 0)
                {
                    var current = open.OrderBy(p => f[p]).First();
                    open.Remove(current);

                    if (Vector3.Distance(current, end) < 0.5f)
                    {
                        var path = new List<Vector3> { end };
                        var c = current;
                        while (came.ContainsKey(c))
                        {
                            path.Add(c);
                            c = came[c];
                        }
                        path.Add(start);
                        path.Reverse();
                        ClearRayCache();
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
                            if (!open.Contains(neighbor)) open.Add(neighbor);
                        }
                    }
                }

                result.Add(end);
                ClearRayCache();
                return result;
            }

            // ---------------- 空间格子缓存 ----------------
            private static Vector2Int ToGrid(Vector3 v)
            {
                return new Vector2Int(Mathf.FloorToInt(v.x / GridSize), Mathf.FloorToInt(v.z / GridSize));
            }

            private static void BuildGridCache(IEnumerable<Vector3> points)
            {
                _gridCache.Clear();
                foreach (var p in points)
                {
                    var g = ToGrid(p);
                    if (!_gridCache.TryGetValue(g, out var list)) _gridCache[g] = list = new List<Vector3>();
                    list.Add(p);
                }
            }

            private static IEnumerable<Vector3> GetNeighborsFromGrid(Vector3 p)
            {
                var gp = ToGrid(p);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var key = new Vector2Int(gp.x + dx, gp.y + dz);
                        if (_gridCache.TryGetValue(key, out var list))
                            foreach (var v in list) yield return v;
                    }
            }

            // ---------------- Physics 缓存 ----------------
            public static bool IsDirectPathClear(Vector3 from, Vector3 to)
            {
                if (_rayCache == null) _rayCache = new Dictionary<(Vector3, Vector3), bool>(64);
                var key = (from, to);
                if (_rayCache.TryGetValue(key, out var ok)) return ok;
                ok = !Physics.Linecast(from, to);
                _rayCache[key] = ok;
                return ok;
            }

            private static void ClearRayCache()
            {
                if (_rayCache == null) return;
                _rayCache.Clear();
            }

            // ---------------- 可行走点获取（仍保留原思路，但减少分配） ----------------
            private static List<Vector3> GetWalkablePoints(Room room)
            {
                var list = new List<Vector3>();
                if (room?.GameObject == null) return list;

                var cols = room.GameObject.GetComponentsInChildren<Collider>(true);
                foreach (var c in cols)
                {
                    if (c is BoxCollider box)
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

            private static bool IsPositionStandable(Vector3 pos)
            {
                // 检查脚下是否可站立
                return Physics.CheckSphere(pos + Vector3.down * 0.1f, 0.3f, -1);
            }

            public List<Room> GetPathRooms(Room start, Room end) => RoomGraph.Instance?.GetRoomPath(start, end);
            public List<Vector3> FindPath(Vector3 start, Vector3 end, Room endRoom) => LocalPathInRoom(start, end, endRoom);
        }
    }

    // ----------------- 基础类型 -----------------
    public class RoomNode
    {
        public Room Room { get; private set; }
        public Vector3 Position => Room.Position;
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

    // ----------------- 更快的优先队列（允许重复项、lazy 去重） -----------------
    public class FastPriorityQueue<T> where T : class
    {
        private readonly List<(T item, float prio)> heap = new List<(T, float)>();
        public int Count => heap.Count;

        public void Enqueue(T item, float prio)
        {
            heap.Add((item, prio));
            int i = heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heap[p].prio <= prio) break;
                var tmp = heap[i]; heap[i] = heap[p]; heap[p] = tmp;
                i = p;
            }
        }

        public T Dequeue()
        {
            var top = heap[0].item;
            var last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return top;
            heap[0] = last;
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, s = i;
                if (l < heap.Count && heap[l].prio < heap[s].prio) s = l;
                if (r < heap.Count && heap[r].prio < heap[s].prio) s = r;
                if (s == i) break;
                var tmp = heap[i]; heap[i] = heap[s]; heap[s] = tmp;
                i = s;
            }
            return top;
        }
    }

    // ----------------- 向后兼容旧接口 -----------------
    public static class RoomGraphExtensions
    {
        public static List<Vector3> LocalPathInRoom(Vector3 start, Vector3 end, Room room)
            => RoomGraph.InternalNav.LocalPathInRoom(start, end, room);

        public static List<Vector3> FindPath(Vector3 start, Vector3 end, Room endRoom)
            => RoomGraph.InternalNav.FindPath(start, end, endRoom);

        public static List<Room> GetPathRooms(Room startRoom, Room endRoom)
            => RoomGraph.InternalNav.GetPathRooms(startRoom, endRoom);

        public static bool IsDirectPathClear(Vector3 from, Vector3 to)
            => RoomGraph.InternalNavigator.IsDirectPathClear(from, to);
    }
}
