using Exiled.API.Features;
using Exiled.API.Features.Doors;
using LabApi.Features.Wrappers;
using MapGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.RectTransform;
using Door = Exiled.API.Features.Doors.Door;
using Room = Exiled.API.Features.Room;

namespace Next_generationSite_27.UnionP
{
    /// <summary>
    /// 房间节点，用于构建房间图
    /// </summary>
    public class RoomNode
    {
        public Room Room { get; }
        public Vector3 Position => Room.Position;
        public HashSet<RoomEdge> Edges { get; } = new HashSet<RoomEdge>();

        public RoomNode(Room room) => Room = room;
    }

    /// <summary>
    /// 房间之间的连接边（通常通过门）
    /// </summary>
    //public class RoomEdge
    //{
    //    public RoomNode From { get; }
    //    public RoomNode To { get; }
    //    public Door Door { get; }
    //    public Vector3 ConnectionPoint => Door.Position;


    //}
    public enum RoomEdgeType
    {
        Door,
        Transition, // 邻近房间之间的“虚拟连接”
        PhysicalConnection
    }

    public class RoomEdge
    {
        public RoomNode From { get; }
        public RoomNode To { get; }
        public Door Door { get; }
        public RoomEdgeType Type { get; }
        public Vector3 ConnectionPoint { get; }

        // 构造函数：用于门
        public RoomEdge(RoomNode from, RoomNode to, Door door, RoomEdgeType type = RoomEdgeType.Door)
        {
            From = from;
            To = to;
            Door = door;
            Type = type;
            ConnectionPoint = door?.Position ?? Vector3.zero;
        }

        // ✅ 构造函数：用于邻近房间过渡
        public RoomEdge(RoomNode from, RoomNode to, Door door, RoomEdgeType type, Vector3 customPoint)
        {
            From = from;
            To = to;
            Door = door;
            Type = type;
            ConnectionPoint = customPoint;
        }
        public RoomEdge(RoomNode from, RoomNode to, Door door)
        {
            From = from;
            To = to;
            Door = door;
        }
    }
    /// <summary>
    /// 优先队列（简易最小堆实现，用于 A*）
    /// </summary>
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly List<(TElement Element, TPriority Priority)> _elements = new List<(TElement Element, TPriority Priority)>();

        public int Count => _elements.Count;

        public void Enqueue(TElement element, TPriority priority)
        {
            _elements.Add((element, priority));
            int i = _elements.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (_elements[i].Priority.CompareTo(_elements[parent].Priority) >= 0)
                    break;
                (_elements[i], _elements[parent]) = (_elements[parent], _elements[i]);
                i = parent;
            }
        }

        public TElement Dequeue()
        {
            var result = _elements[0].Element;
            _elements[0] = _elements[_elements.Count - 1];
            _elements.RemoveAt(_elements.Count - 1);

            int i = 0;
            while (true)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < _elements.Count && _elements[left].Priority.CompareTo(_elements[smallest].Priority) < 0)
                    smallest = left;
                if (right < _elements.Count && _elements[right].Priority.CompareTo(_elements[smallest].Priority) < 0)
                    smallest = right;

                if (smallest == i) break;

                (_elements[i], _elements[smallest]) = (_elements[smallest], _elements[i]);
                i = smallest;
            }

            return result;
        }

        public bool UnionWithPriority(TElement element, TPriority priority)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                if (EqualityComparer<TElement>.Default.Equals(_elements[i].Element, element))
                {
                    if (priority.CompareTo(_elements[i].Priority) < 0)
                    {
                        _elements[i] = (element, priority);
                    }
                    return true;
                }
            }
            return false;
        }
    }
    public readonly struct RoomPair : IEquatable<RoomPair>
    {
        public readonly RoomIdentifier First;
        public readonly RoomIdentifier Second;

        public RoomPair(RoomIdentifier a, RoomIdentifier b)
        {
            // ✅ 使用 MainCoords 代替 Id —— 它是 Vector3Int，可比较
            if (CompareCoords(a.MainCoords, b.MainCoords) < 0)
            {
                First = a;
                Second = b;
            }
            else
            {
                First = b;
                Second = a;
            }
        }

        private static int CompareCoords(Vector3Int a, Vector3Int b)
        {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.z.CompareTo(b.z);
        }

        public bool Equals(RoomPair other)
        {
            return First == other.First && Second == other.Second;
        }

        public override int GetHashCode()
        {
            // ✅ 手动实现哈希，避免使用 .NET 5+ 的 HashCode.Combine（SCP:SL 是 .NET Framework）
            return First.GetHashCode() * 397 ^ Second.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is RoomPair other && Equals(other);
        }
    }
    /// <summary>
    /// 房间关系图（预处理构建）
    /// </summary>
    public class RoomGraph
    {
        public Dictionary<Room, RoomNode> Nodes { get; } = new Dictionary<Room, RoomNode>();
        public void Build()
        {
            Nodes.Clear();
            var connectedPairs = new HashSet<RoomPair>(); // 👈 新增：记录已连接的房间对

            // 创建所有房间节点
            foreach (var room in Room.List)
            {
                Nodes[room] = new RoomNode(room);
            }

            // 第一阶段：通过门建立连接（主连接）
            foreach (var door in Door.List)
            {
                var roomsList = door.Rooms?.ToList();
                if (roomsList == null || roomsList.Count < 2) continue;

                var roomA = roomsList[0];
                var roomB = roomsList[1];

                if (!Nodes.TryGetValue(roomA, out RoomNode nodeA) ||
                    !Nodes.TryGetValue(roomB, out RoomNode nodeB)) continue;

                var edgeAB = new RoomEdge(nodeA, nodeB, door, RoomEdgeType.Door);
                var edgeBA = new RoomEdge(nodeB, nodeA, door, RoomEdgeType.Door);

                nodeA.Edges.Add(edgeAB);
                nodeB.Edges.Add(edgeBA);

                // 👇 记录这对房间“已连接”
                connectedPairs.Add(new RoomPair(roomA.Identifier, roomB.Identifier));
            }

            // 第二阶段：使用 ConnectedRooms 建立“物理连通”边（兜底）
            foreach (var room in Room.List)
            {
                if (!Nodes.TryGetValue(room, out RoomNode nodeA)) continue;

                var connectedRooms = room.NearestRooms; // 或者你用的 ConnectedRooms
                if (connectedRooms == null) continue;

                foreach (var neighborRoom in connectedRooms)
                {
                    if (neighborRoom == room) continue;
                    if (!Nodes.TryGetValue(neighborRoom, out RoomNode nodeB)) continue;

                    // ✅ 关键：检查是否已经通过 Door 连接
                    var pair = new RoomPair(room.Identifier, neighborRoom.Identifier);
                    if (connectedPairs.Contains(pair))
                    {
                        continue; // 已有 Door 连接，跳过
                    }

                    // 尝试找过渡点
                    if (TryFindTransitionPoint(room, neighborRoom, out Vector3 transitionPoint))
                    {
                        var edgeAB = new RoomEdge(nodeA, nodeB, null, RoomEdgeType.Transition, transitionPoint);
                        var edgeBA = new RoomEdge(nodeB, nodeA, null, RoomEdgeType.Transition, transitionPoint);

                        nodeA.Edges.Add(edgeAB);
                        nodeB.Edges.Add(edgeBA);

                        // 👇 可选：也记录这个兜底连接，防止后续重复（如果 NearestRooms 有重复）
                        connectedPairs.Add(pair);
                    }
                }
            }
        }
        private bool TryFindTransitionPoint(Room roomA, Room roomB, out Vector3 point)
        {
            point = Vector3.zero;

            var connectedRoomsA = roomA.Identifier?.ConnectedRooms?
      .Select(ri => Room.Get(ri)) // 👈 关键：把 RoomIdentifier 转成 Room
      .Where(r => r != null) // 过滤掉 null
      .ToList() ?? new List<Room>();

            var connectedRoomsB = roomB.Identifier?.ConnectedRooms?
                .Select(ri => Room.Get(ri))
                .Where(r => r != null)
                .ToList() ?? new List<Room>();


            // 合并为一个“允许区域”集合
            var allowedRooms = new HashSet<Room>(connectedRoomsA) { roomA };
            foreach (var r in connectedRoomsB) allowedRooms.Add(r);
            allowedRooms.Add(roomB); // 确保包含 roomB

            Vector3 start = roomA.Position;
            Vector3 end = roomB.Position;
            float distance = Vector3.Distance(start, end);

            // 如果距离太近，直接用中点（避免采样不足）
            if (distance < 1f)
            {
                Vector3 mid = (start + end) * 0.5f;
                mid.y += 0.1f; // 站立高度微调
                if (IsPositionStandable(mid) && IsInAllowedRoom(mid, allowedRooms))
                {
                    point = mid;
                    return true;
                }
            }

            // ✅ 增加采样点数量（10个点）
            int samples = 50;
            for (int i = 1; i < samples; i++)
            {
                float t = i / (float)samples;
                Vector3 sample = Vector3.Lerp(start, end, t);
                sample.y += 0.1f; // 👈 更安全的站立高度偏移

                // ✅ 检查是否可站立
                if (!IsPositionStandable(sample))
                    continue;

                // ✅ 检查是否在“允许的连通房间集合”内（不再仅限于A或B！）
                if (!IsInAllowedRoom(sample, allowedRooms))
                    continue;

                // ✅ 检查从A或B到该点是否路径畅通
                if (IsDirectPathClear(start, sample) || IsDirectPathClear(end, sample))
                {
                    point = sample;
                    return true;
                }
            }

            // ❗ 调试日志：帮助你定位为什么失败
            Log.Debug($"❌ TryFindTransitionPoint 失败: {roomA.Type} ({roomA.Zone}) <-> {roomB.Type} ({roomB.Zone})");

            return false;
        }

        private bool IsInAllowedRoom(Vector3 pos, HashSet<Room> allowedRooms)
        {
            Room roomHere = Room.Get(pos);
            return roomHere != null && allowedRooms.Contains(roomHere);
        }

        private static bool IsPositionStandable(Vector3 pos)
        {
            // ✅ 启用真实检测！检测脚下是否有地面
            return Physics.CheckSphere(pos + Vector3.down * 0.1f, 0.3f, -1);
        }

        private static bool IsDirectPathClear(Vector3 from, Vector3 to)
        {
            // ✅ 启用真实检测！检测是否穿墙
            // 检测两个高度，避免贴地或贴天花板穿模
            bool clearLow = !Physics.Linecast(from + Vector3.up * 0.5f, to + Vector3.up * 0.5f, -1);
            bool clearHigh = !Physics.Linecast(from + Vector3.up * 1.0f, to + Vector3.up * 1.0f, -1);
            return clearLow || clearHigh;
        }
        /// <summary>
        /// 房间级 A* 寻路器 + 局部避障路径生成
        /// </summary>
        public class SimpleRoomNavigation
        {
            private RoomGraph _graph;
            public Room FindNearestRoom(Vector3 startPos)
            {
                // 获取所有房间，并按距离升序排序，选择第一个作为最近房间
                return Room.List.OrderBy(room => Vector3.Distance(startPos, room.Position)).FirstOrDefault();
            }
            public SimpleRoomNavigation()
            {
                _graph = new RoomGraph();
                _graph.Build();
                if(Nav == null)
                {
                    Nav = this;
                }
            }
            static public SimpleRoomNavigation Nav = null;
            /// <summary>
            /// 重新构建房间图（地图重载后调用）
            /// </summary>
            public void RebuildGraph() => _graph.Build();

            public List<Vector3> FindPath(Vector3 start, Vector3 end, Room endRoom)
            {
                return FindPath(start, FindNearestRoom(start), end, endRoom);
            }

            /// <summary>
            /// 寻找从起点到终点的完整路径（跨房间）
            /// </summary>
            public List<Vector3> FindPath(Vector3 start, Room startRoom, Vector3 end, Room endRoom)
            {
                if (startRoom == null || endRoom == null)
                {
                    //Log.Info("startend null");
                    return null;
                }
                ;

                if (startRoom == endRoom)
                {
                    return LocalPathInRoom(start, end, startRoom);
                }

                if (!_graph.Nodes.TryGetValue(startRoom, out var startNode) ||
                    !_graph.Nodes.TryGetValue(endRoom, out var endNode))
                    return null;

                var openSet = new PriorityQueue<RoomNode, float>();
                var cameFrom = new Dictionary<RoomNode, RoomEdge>();
                var gScore = new Dictionary<RoomNode, float>();
                var fScore = new Dictionary<RoomNode, float>();

                foreach (var node in _graph.Nodes.Values)
                {
                    gScore[node] = float.MaxValue;
                    fScore[node] = float.MaxValue;
                }

                gScore[startNode] = 0;
                fScore[startNode] = Heuristic(startNode, endNode);
                openSet.Enqueue(startNode, fScore[startNode]);

                while (openSet.Count > 0)
                {
                    var current = openSet.Dequeue();

                    //Log.Info(current);
                    if (current == endNode)
                    {
                        return ReconstructPath(cameFrom, current, start, end, startRoom, endRoom);
                    }

                    foreach (var edge in current.Edges)
                    {
                        if (!IsEdgePassable(edge)) continue;

                        var neighbor = edge.To;
                        // ✅ 关键修改：使用门的位置作为路径点，而不是房间中心
                        var tentativeG = gScore[current] + Vector3.Distance(current.Position, edge.ConnectionPoint);

                        if (tentativeG < gScore[neighbor])
                        {
                            cameFrom[neighbor] = edge; // 记录通过哪扇门到达
                            gScore[neighbor] = tentativeG;
                            fScore[neighbor] = tentativeG + Heuristic(neighbor, endNode);


                            if (!openSet.UnionWithPriority(neighbor, fScore[neighbor]))
                                openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }

                //Log.Info("final null");
                return null; // 无路径
            }

            private float Heuristic(RoomNode a, RoomNode b)
            {
                return Vector3.Distance(a.Position, b.Position);
            }
            public List<Room> GetPathRooms(Room startRoom, Room endRoom)
            {
                if (startRoom == null || endRoom == null) return null;

                if (startRoom == endRoom)
                {
                    return new List<Room> { startRoom };
                }

                if (!_graph.Nodes.TryGetValue(startRoom, out RoomNode startNode) ||
                    !_graph.Nodes.TryGetValue(endRoom, out RoomNode endNode))
                    return null;

                var openSet = new PriorityQueue<RoomNode, float>();
                var cameFrom = new Dictionary<RoomNode, RoomEdge>();
                var gScore = new Dictionary<RoomNode, float>();
                var fScore = new Dictionary<RoomNode, float>();

                foreach (var node in _graph.Nodes.Values)
                {
                    gScore[node] = float.MaxValue;
                    fScore[node] = float.MaxValue;
                }

                gScore[startNode] = 0;
                fScore[startNode] = Heuristic(startNode, endNode);
                openSet.Enqueue(startNode, fScore[startNode]);

                while (openSet.Count > 0)
                {
                    var current = openSet.Dequeue();

                    if (current == endNode)
                    {
                        return ReconstructRoomPath(cameFrom, current, startNode);
                    }

                    foreach (var edge in current.Edges)
                    {
                        if (!IsEdgePassable(edge)) continue;

                        var neighbor = edge.To;
                        var tentativeG = gScore[current] + Vector3.Distance(current.Position, edge.ConnectionPoint);

                        if (tentativeG < gScore[neighbor])
                        {
                            cameFrom[neighbor] = edge;
                            gScore[neighbor] = tentativeG;
                            fScore[neighbor] = tentativeG + Heuristic(neighbor, endNode);

                            if (!openSet.UnionWithPriority(neighbor, fScore[neighbor]))
                                openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }

                return null; // 无路径
            }
            private List<Room> ReconstructRoomPath(Dictionary<RoomNode, RoomEdge> cameFrom, RoomNode endNode, RoomNode startNode)
            {
                var path = new List<Room>();
                var current = endNode;

                // 反向追溯
                var stack = new Stack<Room>();
                stack.Push(current.Room);

                while (current != startNode)
                {
                    if (!cameFrom.TryGetValue(current, out RoomEdge edge))
                        return null; // 路径断裂

                    current = edge.From;
                    stack.Push(current.Room);
                }

                // 反转得到正向路径
                while (stack.Count > 0)
                {
                    path.Add(stack.Pop());
                }

                return path;
            }
            private List<Vector3> ReconstructPath(Dictionary<RoomNode, RoomEdge> cameFrom, RoomNode endNode,
        Vector3 start, Vector3 end, Room startRoom, Room endRoom)
            {
                var totalPath = new List<Vector3>();
                totalPath.Add(start); // 起点

                // 获取完整路径上的所有边（门）
                var edges = new List<RoomEdge>();
                var current = endNode;

                while (cameFrom.TryGetValue(current, out RoomEdge edge))
                {
                    edges.Add(edge);
                    current = edge.From;
                }

                // 反转，从起点房间开始
                edges.Reverse();

                // 第一段：起点 → 第一扇门
                if (edges.Count > 0)
                {
                    var firstDoorPos = edges[0].ConnectionPoint;
                    var pathToFirstDoor = LocalPathInRoom(start, firstDoorPos, startRoom);
                    if (pathToFirstDoor != null)
                        totalPath.AddRange(pathToFirstDoor.Skip(1)); // Skip(1) 避免重复起点
                }

                // 中间段：门 → 门（经过多个房间）
                for (int i = 0; i < edges.Count - 1; i++)
                {
                    var currentDoor = edges[i];
                    var nextDoor = edges[i + 1];

                    // 在当前房间内：从当前门 → 下一扇门
                    var localPath = LocalPathInRoom(
                        currentDoor.ConnectionPoint,
                        nextDoor.ConnectionPoint,
                        currentDoor.To.Room); // 注意：当前门的“To”房间就是路径所在的房间

                    if (localPath != null)
                        totalPath.AddRange(localPath.Skip(1));
                }

                // 最后一段：最后一扇门 → 终点
                if (edges.Count > 0)
                {
                    var lastDoorPos = edges[edges.Count - 1].ConnectionPoint; // C# 8+ 语法，如不支持请用 edges[edges.Count - 1]
                    var pathToEnd = LocalPathInRoom(lastDoorPos, end, endRoom);
                    if (pathToEnd != null)
                        totalPath.AddRange(pathToEnd.Skip(1));
                }
                else
                {
                    // 同房间情况
                    var directPath = LocalPathInRoom(start, end, startRoom);
                    if (directPath != null)
                        totalPath = directPath;
                }

                return totalPath;
            }
            private bool IsEdgePassable(RoomEdge edge)
            {
                if (edge.Type == RoomEdgeType.Door)
                {
                    var door = edge.Door;
                    return door.IsOpen || door.IsElevator ||
       door.Type == Exiled.API.Enums.DoorType.Scp079First ||
       door.IsCheckpoint;
                }

                if (edge.Type == RoomEdgeType.Transition)
                {
                    // ✅ 过渡边默认可通行（已在构建时验证过）
                    return true;
                }

                return false;
            }
            static CachedLayerMask RoomDetectionMask = new CachedLayerMask(new string[]
        {
            "Default",
            "InvisibleCollider",
            "Fence",
            "Glass",
            //"Door"
        });
            public static bool IsDirectPathClear(Vector3 from, Vector3 to)
            {
                // 射线检测是否穿墙
                //return !Physics.Linecast(from + Vector3.up * 0.5f, to + Vector3.up * 0.5f, RoomDetectionMask.Mask) || !Physics.Linecast(from + Vector3.up, to + Vector3.up, RoomDetectionMask.Mask) || !Physics.Linecast(from + Vector3.up, to + Vector3.up, RoomDetectionMask.Mask)
                //    || !Physics.Linecast(from, to) || !Physics.Linecast(from + Vector3.up, to + Vector3.up, RoomDetectionMask.Mask);
                return !Physics.Linecast(from, to, RoomDetectionMask.Mask) && !Physics.Linecast(from + Vector3.up * 0.5f, to + Vector3.up * 0.5f, RoomDetectionMask.Mask);
            }

            /// <summary>
            /// 在单个房间内生成局部路径（避墙）
            /// </summary>
            private static readonly Dictionary<Room, List<Vector3>> WalkablePointCache = new Dictionary<Room, List<Vector3>>();

            /// <summary>
            /// 在房间内生成从 start 到 end 的局部路径（避开障碍物）
            /// </summary>
            public static List<Vector3> LocalPathInRoom(Vector3 start, Vector3 end, Room room)
            {
                var path = new List<Vector3> { start };

                // 如果直线无阻挡，直接走
                if (IsDirectPathClear(start, end))
                {
                    path.Add(end);
                    //Log.Debug($"🧱 直走");
                    return path;
                }

                // 获取该房间的地面可行走点
                var walkablePoints = GetWalkablePoints(room);

                // 尝试用可行走点作为中转
                foreach (var point in walkablePoints)
                {
                    if (IsPositionStandable(point) &&
                        IsDirectPathClear(start, point) &&
                        IsDirectPathClear(point, end))
                    {
                        path.Add(point);
                        path.Add(end);
                        //Log.Debug($"✅ LocalPathInRoom: 使用地面点 {point} 成功绕障");
                        return path;
                    }
                }

                // ❗ 地面点失败，fallback 随机采样
                //Log.Debug($"⚠️ LocalPathInRoom: 地面点失败，尝试随机采样...");

                var attempts = 100;
                for (int i = 0; i < attempts; i++)
                {
                    var mid = Vector3.Lerp(start, end, 0.5f) + UnityEngine.Random.insideUnitSphere * 3f;
                    mid.y = start.y; // 保持高度一致

                    if (IsPositionStandable(mid) &&
                        IsDirectPathClear(start, mid) &&
                        IsDirectPathClear(mid, end))
                    {
                        path.Add(mid);
                        path.Add(end);
                        //Log.Debug($"✅ LocalPathInRoom: 随机点 {mid} 成功绕障");/
                        return path;
                    }
                }

                // 最后手段：直线（可能穿墙）
                //Log.Debug($"❌ LocalPathInRoom: 所有绕障失败，使用直线（可能穿墙）");
                path.Add(end);
                return path;
            }

            /// <summary>
            /// 获取房间内所有“地面”三角形的中心点（可行走区域）
            /// </summary>
            private static List<Vector3> GetWalkablePoints(Room room)
            {
                if (room == null || room.GameObject == null)
                    return new List<Vector3>();

                // 尝试从缓存读取
                if (WalkablePointCache.TryGetValue(room, out var cachedPoints))
                {
                    return cachedPoints;
                }

                var walkablePoints = new List<Vector3>();

                // 遍历房间内所有 MeshFilter
                foreach (var collider in room.GameObject.GetComponentsInChildren<Collider>(true))
                {
                    // ✅ 只处理 MeshCollider（其他类型如 BoxCollider 需要特殊处理）
                    switch (collider)
                    {
                        case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                            {
                                var mesh = meshCollider.sharedMesh;
                                var vertices = mesh.vertices;
                                var triangles = mesh.triangles;

                                for (int i = 0; i < triangles.Length; i += 3)
                                {
                                    try
                                    {
                                        Vector3 v1 = collider.transform.TransformPoint(vertices[triangles[i]]);
                                        Vector3 v2 = collider.transform.TransformPoint(vertices[triangles[i + 1]]);
                                        Vector3 v3 = collider.transform.TransformPoint(vertices[triangles[i + 2]]);

                                        // 计算法线，判断坡度
                                        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                                        float slopeAngle = Vector3.Angle(normal, Vector3.up);
                                        if (slopeAngle > 45f) continue; // 太陡跳过

                                        Vector3 center = (v1 + v2 + v3) / 3f;
                                        walkablePoints.Add(center);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"处理 MeshCollider 三角形时出错: {ex.Message}");
                                    }
                                }
                            }
                            break;

                        case BoxCollider boxCollider:
                            // ✅ 采样 Box 的 8 个角点 + 中心点
                            var corners = GetBoxCorners(boxCollider);
                            foreach (var corner in corners)
                            {
                                if (IsPositionStandable(corner)) // 可选：再验证是否可站立
                                    walkablePoints.Add(corner);
                            }
                            break;

                        case SphereCollider sphereCollider:
                            // ✅ 采样球体底部点
                            Vector3 Sbottom = collider.transform.position - Vector3.up * sphereCollider.radius;
                            walkablePoints.Add(Sbottom);
                            break;

                        case CapsuleCollider capsuleCollider:
                            // ✅ 采样胶囊底部
                            float height = capsuleCollider.height * 0.5f;
                            Vector3 bottom = collider.transform.position - Vector3.up * height;
                            walkablePoints.Add(bottom);
                            break;
                    }
                    // 将 C# 9.0 的 "not" 模式替换为 C# 7.3 兼容写法
                    // 原代码：if (collider is not MeshCollider meshCollider || meshCollider.sharedMesh == null)
                }

                // 缓存结果（房间结构不会变，可安全缓存）
                WalkablePointCache[room] = walkablePoints;

                Log.Debug($"🧱 房间 {room.Type} 提取到 {walkablePoints.Count} 个地面点");
                return walkablePoints;
            }
            private static Vector3[] GetBoxCorners(BoxCollider box)
            {
                Vector3 center = box.center;
                Vector3 extents = box.size * 0.5f;

                var corners = new Vector3[8];
                int i = 0;
                for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 local = center + new Vector3(x * extents.x, y * extents.y, z * extents.z);
                            corners[i++] = box.transform.TransformPoint(local);
                        }
                return corners;
            }
            /// <summary>
            /// 检测位置是否可站立（脚下有地面）
            /// </summary>
            private static bool IsPositionStandable(Vector3 pos)
            {
                return Physics.CheckSphere(pos + Vector3.down * 0.1f, 0.3f, -1);
            }
        }
    }
}