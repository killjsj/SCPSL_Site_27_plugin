using Exiled.API.Features;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace Next_generationSite_27
{
    public class MySQLConnect
    {
        private MySqlConnection connection;
        private string connectionString;
        public bool connected = false;

        /// <summary>
        /// 连接到 MySQL 数据库
        /// </summary>
        /// <param name="connectionString">MySQL 连接字符串</param>
        public void Connect(string connectionString)
        {
            this.connectionString = connectionString;

            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();
                connection.Close();
                connected = true;
                Log.Info("✅ 数据库连接成功。");
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 连接数据库失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 查询指定用户的 Snack 最高分和记录时间
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns>(name, highscore, Snack_Create_time)</returns>
        public (string name, int? highscore, DateTime? time) Query(string userid)
        {
            if (!connected)
                return (string.Empty, 0, null);

            // 从 user 表直接读取 highscore 和 Snack_Create_time
            string query = @"
                SELECT 
                    name, 
                    highscore, 
                    Snack_Create_time 
                FROM user 
                WHERE userid = @userid";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int timeOrdinal = reader.GetOrdinal("Snack_Create_time");

                            string name = reader["name"].ToString();
                            int highscore = reader.GetInt32("highscore");
                            DateTime? time = reader.IsDBNull(timeOrdinal)
                                ? (DateTime?)null
                                : reader.GetDateTime(timeOrdinal);

                            return (name, highscore, time);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的 Snack 分数失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            // 用户不存在或无数据
            return (string.Empty, 0, null);
        }
        //public (string name,int level, int exp) QueryUser(string userid)
        //{
        //    if (!connected)
        //        return (string.Empty, 0, null);

        //    // 从 user 表直接读取 highscore 和 Snack_Create_time
        //    string query = @"
        //        SELECT 
        //            name, 
        //            highscore, 
        //            Snack_Create_time 
        //        FROM user 
        //        WHERE userid = @userid";

        //    try
        //    {
        //        connection.Open();
        //        using (var cmd = new MySqlCommand(query, connection))
        //        {
        //            cmd.Parameters.AddWithValue("@userid", userid);
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                if (reader.Read())
        //                {
        //                    int timeOrdinal = reader.GetOrdinal("Snack_Create_time");

        //                    string name = reader["name"].ToString();
        //                    int highscore = reader.GetInt32("highscore");
        //                    DateTime? time = reader.IsDBNull(timeOrdinal)
        //                        ? (DateTime?)null
        //                        : reader.GetDateTime(timeOrdinal);

        //                    return (name, highscore, time);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"❌ 查询用户 {userid} 的 Snack 分数失败: {ex.Message}");
        //    }
        //    finally
        //    {
        //        if (connection.State == ConnectionState.Open)
        //            connection.Close();
        //    }

        //    // 用户不存在或无数据
        //    return (string.Empty, 0, null);
        //}
        public (string name, string welcomeText, string color,bool enabled) QueryCassieWelcome(string userid)
        {
            if (!connected)
                return (string.Empty, null, null,false);

            string query = @"
        SELECT 
            name, 
            welcomeText, 
            color 
        FROM cassie_welcome 
        WHERE userID = @userid";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader["name"].ToString();
                            string welcomeText = reader["welcomeText"] as string; // 可为 null
                            string color = reader["color"] as string;           // 可为 null
                            string displayColor = string.IsNullOrEmpty(color) ? "white" : color;
                            return (name, welcomeText, displayColor, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的卡西欢迎配置失败: {ex.Message}");

            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            // 用户未在 cassie_welcome 表中，视为【未启用】卡西播报
            return (string.Empty, null, null, false);
        }
        public List<(string name, string card, string Text, string holder, string color, string permColor,byte? rankLevel, string CardName,bool ApplytoAll, bool enabled)> QueryCard(string userid)
        {
            var l = new List<(string name, string card, string Text, string holder, string color, string permColor, byte? rankLevel, string CardName, bool ApplytoAll, bool enabled)>();
            if (!connected)
                return l;

            string query = @"
        SELECT 
            name, 
            card,
            Text, 
            holder,
            color,
            rankLevel,
            permColor,
            applytoAll,
            Cardname
        FROM card 
        WHERE userID = @userid
        ORDER BY id DESC";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader["name"].ToString();
                            string holder = reader["holder"] as string; // 可为 null
                            string Text = reader["Text"] as string; // 可为 null
                            string card = reader["card"] as string; // 可为 null
                            string color = reader["color"] as string;           // 可为 null
                            string permColor = reader["permColor"] as string;           // 可为 null
                            byte? rankLevel = reader["rankLevel"] as byte?;           // 可为 null
                            int? applytoAll = reader["applytoAll"] as int?;           // 可为 null
                            string Cardname = reader["Cardname"] as string;           // 可为 null
                            bool ApplytoAll = applytoAll.GetValueOrDefault(0) == 1;
                            string displayColor = string.IsNullOrEmpty(color) ? "white" : color;
                            string displayPermColor = string.IsNullOrEmpty(permColor) ? "white" : permColor;
                            string displayCardname = string.IsNullOrEmpty(Cardname) ? "site27 自定义权限卡" : Cardname;
                            l.Add( (name, card,Text,holder, displayColor, displayPermColor, rankLevel, displayCardname, ApplytoAll, true));
                        }
                    }
                    return l;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的卡失败: {ex.Message}");

            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
            return l;

        }
        /// 更新用户在 user 表中的最高分记录（仅当新分数更高时）
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="name">玩家名称</param>
        /// <param name="highscore">本次得分</param>
        /// <param name="time">本次游戏时间</param>
        public void Update(string userid, string name, int highscore, DateTime time)
        {
            if (!connected) return;

            // 仅当新分数更高时，才更新 highscore 和 Snack_Create_time
            string query = @"
UPDATE user 
SET 
    name = @name,
    highscore = @highscore,
    Snack_Create_time = @snack_create_time
WHERE userid = @userid;";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    cmd.Parameters.AddWithValue("@name", name ?? string.Empty); // 提供默认值
                    cmd.Parameters.AddWithValue("@highscore", highscore);
                    cmd.Parameters.AddWithValue("@snack_create_time", time);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    // 如果没有更新（分数不高），可以插入新用户（如果用户不存在）
                    if (rowsAffected == 0)
                    {
                        InsertOrUpdateUser(cmd, userid, name, highscore, time);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"更新用户 {userid} 的 Snack 分数失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// 插入新用户或更新（当分数不更高但用户存在时）
        /// </summary>
        private void InsertOrUpdateUser(MySqlCommand cmd, string userid, string name, int highscore, DateTime time)
        {
            // 先尝试插入（如果用户不存在）
            string insertSql = @"
INSERT INTO user (userid, name, highscore, Snack_Create_time)
VALUES (@userid, @name, @highscore, @snack_create_time)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    highscore = GREATEST(highscore, VALUES(highscore)),
    Snack_Create_time = CASE WHEN VALUES(highscore) > highscore THEN VALUES(Snack_Create_time) ELSE Snack_Create_time END";

            cmd.CommandText = insertSql;
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@userid", userid);
            cmd.Parameters.AddWithValue("@name", name ?? string.Empty); // 提供默认值
            cmd.Parameters.AddWithValue("@highscore", highscore);
            cmd.Parameters.AddWithValue("@snack_create_time", time);
            cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// 查询全局最高分记录
        /// </summary>
        /// <returns>(userid, name, highscore, Snack_Create_time)</returns>
        public (string userid, string name, int? highscore, DateTime? time) QueryHighest()
        {
            if (!connected)
                return (string.Empty, string.Empty, 0, null);

            string query = @"
                SELECT 
                    userid, 
                    name, 
                    highscore, 
                    Snack_Create_time 
                FROM user 
                WHERE highscore > 0 
                ORDER BY highscore DESC 
                LIMIT 1";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int timeOrdinal = reader.GetOrdinal("Snack_Create_time");

                        return (
                            reader["userid"].ToString(),
                            reader["name"].ToString(),
                            reader.GetInt32("highscore"),
                            reader.IsDBNull(timeOrdinal)
                                ? (DateTime?)null
                                : reader.GetDateTime(timeOrdinal)
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询最高分失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return (string.Empty, string.Empty, 0, null);
        }
        /// <summary>
        /// 查询 Snack 游戏排行榜前 N 名
        /// </summary>
        /// <param name="rankcount">要查询的排名数量（如 10）</param>
        /// <returns>排名列表：(rank, userid, name, highscore, Snack_Create_time)</returns>
        public List<(int rank, string userid, string name, int highscore, DateTime? time)> GetTopSnackScores(int rankcount)
        {
            if (!connected)
                return new List<(int, string, string, int, DateTime?)>();

            // 限制最大返回数量，防止内存溢出
            int limit = Math.Max(1, Math.Min(rankcount, 100)); // 最多返回 100 条

            string query = @"
        SELECT 
            userid,
            name,
            highscore,
            Snack_Create_time 
        FROM user 
        WHERE highscore > 0 
        ORDER BY highscore DESC 
        LIMIT @limit";

            var result = new List<(int rank, string userid, string name, int highscore, DateTime? time)>();

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@limit", limit);
                    using (var reader = cmd.ExecuteReader())
                    {
                        int rank = 1;
                        while (reader.Read())
                        {
                            int timeOrdinal = reader.GetOrdinal("Snack_Create_time");
                            string userid = reader["userid"].ToString();
                            string name = reader["name"].ToString();
                            int highscore = reader.GetInt32("highscore");
                            DateTime? time = reader.IsDBNull(timeOrdinal)
                                    ? (DateTime?)null
                                    : reader.GetDateTime(timeOrdinal);

                            result.Add((rank++, userid, name, highscore, time));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询前 {limit} 名排行榜失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return result;
        }
        public void LogAdminPermission(string userid, string name, int port, string command, string result, string additionalInfo = "", string group ="")
        {
            if (!connected) return;

            string query = @"
                INSERT INTO admin_log 
                (userid, name, operation_time, port, command_name, command_result, additional_info,admingrooup)
                VALUES 
                (@userid, @name, @operation_time, @port, @command_name, @command_result, @additional_info,@admingrooup)";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid ?? string.Empty);
                    cmd.Parameters.AddWithValue("@name", name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@operation_time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@port", port);
                    cmd.Parameters.AddWithValue("@command_name", command ?? string.Empty);
                    cmd.Parameters.AddWithValue("@command_result", result ?? string.Empty);
                    cmd.Parameters.AddWithValue("@additional_info", additionalInfo ?? string.Empty);
                    cmd.Parameters.AddWithValue("@admingrooup", group ?? string.Empty);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 记录管理员权限日志失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void Close()
        {
            connection?.Close();
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~MySQLConnect()
        {
            Close();
        }
    }
}
