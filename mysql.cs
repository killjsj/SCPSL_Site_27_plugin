using Cryptography;
using Exiled.API.Features;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Xml.Linq;
using static Mysqlx.Notice.Warning.Types;

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
        public (string name, int? highscore, DateTime? time) QuerySnake(string userid)
        {
            if (!connected)
                return (string.Empty, null, null);

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
                            int nameOrdinal = reader.GetOrdinal("name");
                            int highscoreOrdinal = reader.GetOrdinal("highscore");
                            int timeOrdinal = reader.GetOrdinal("Snack_Create_time");

                            string name = reader.IsDBNull(nameOrdinal) ? string.Empty : reader.GetString(nameOrdinal);
                            int? highscore = reader.IsDBNull(highscoreOrdinal) ? (int?)null : reader.GetInt32(highscoreOrdinal);
                            DateTime? time = reader.IsDBNull(timeOrdinal) ? (DateTime?)null : reader.GetDateTime(timeOrdinal);

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
            return (string.Empty, null, null);
        }
        public (int uid, string name, int level, int experience,double? experience_multiplier, int point, string ip, DateTime? last_time, TimeSpan? total_duration, TimeSpan? today_duration) QueryUser(string userid)
        {
            if (!connected)
                return (0, null, 0, 0,0,1,null, null, null, null);

            string query = @"
        SELECT 
            uid,
            name,
            userid,
            level,
            experience,
            experience_multiplier,
            point,
            ip,
            today_duration,
            total_duration,
            last_time
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
                            int uidOrdinal = reader.GetOrdinal("uid");
                            int nameOrdinal = reader.GetOrdinal("name");
                            int levelOrdinal = reader.GetOrdinal("level");
                            int experienceOrdinal = reader.GetOrdinal("experience");
                            int experience_multiplierOrdinal = reader.GetOrdinal("experience_multiplier");
                            int PointOrdinal = reader.GetOrdinal("point");
                            int ipOrdinal = reader.GetOrdinal("ip");
                            int today_durationOrdinal = reader.GetOrdinal("today_duration");
                            int total_durationOrdinal = reader.GetOrdinal("total_duration");
                            int last_timeOrdinal = reader.GetOrdinal("last_time");

                            int uid = reader.IsDBNull(nameOrdinal) ? 0 : reader.GetInt32(uidOrdinal);
                            string name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal);
                            int level = reader.IsDBNull(levelOrdinal) ? 0 : reader.GetInt32(levelOrdinal);
                            int experience = reader.IsDBNull(experienceOrdinal) ? 0 : reader.GetInt32(experienceOrdinal);
                            double? experience_multiplier = reader.IsDBNull(experience_multiplierOrdinal) ? (double?)null : reader.GetDouble(experience_multiplierOrdinal);
                            int point = reader.IsDBNull(PointOrdinal) ? 0: reader.GetInt32(PointOrdinal);
                            string ip = reader.IsDBNull(ipOrdinal) ? "1.1.1.1" : reader.GetString(ipOrdinal);
                            DateTime? last_time = reader.IsDBNull(last_timeOrdinal) ? (DateTime?)null : reader.GetDateTime(last_timeOrdinal);
                            TimeSpan? today_duration = reader.IsDBNull(today_durationOrdinal) ? (TimeSpan?)null : reader.GetTimeSpan(today_durationOrdinal);
                            TimeSpan? total_duration = reader.IsDBNull(total_durationOrdinal) ? (TimeSpan?)null : reader.GetTimeSpan(total_durationOrdinal);

                            return (uid, name, level, experience, experience_multiplier, point, ip, last_time, total_duration, today_duration);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
            return (0, null, 0, 0, 0, 1, null, null, null, null);
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
        public (string name, string welcomeText, string color, bool enabled) QueryCassieWelcome(string userid)
        {
            if (!connected)
                return (string.Empty, null, null, false);

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
        public List<(string name, string card, string Text, string holder, string color, string permColor, byte? rankLevel, string CardName, bool ApplytoAll, bool enabled)> QueryCard(string userid)
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
                            l.Add((name, card, Text, holder, displayColor, displayPermColor, rankLevel, displayCardname, ApplytoAll, true));
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
        public void Update(
            string userid,
            string name = null,
            int level = -1,
            int experience = -1,
            double? experience_multiplier = null,
            string ip = null,
            int point = -1,
            DateTime? last_time = null,
            TimeSpan? total_duration = null,
            TimeSpan? today_duration = null)
        {
            if (!connected || string.IsNullOrEmpty(userid)) return;

            try
            {
                    var p = QueryUser(userid);
                connection.Open();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = connection;

                    // 先查询现有用户数据（仅当需要 fallback 时）

                    // 填充默认值（来自数据库）
                    name = name ?? p.name;
                    level = level == -1 ? p.level : level;
                    point = point == -1 ? p.point : point;
                    experience = experience == -1 ? p.experience : experience;
                    experience_multiplier = experience_multiplier ?? p.experience_multiplier;
                    ip = ip ?? p.ip;
                    last_time = last_time ?? p.last_time;
                    total_duration = total_duration ?? p.total_duration;
                    today_duration = today_duration ?? p.today_duration;

                    // 使用 INSERT ... ON DUPLICATE KEY UPDATE
                    string upsertSql = @"
INSERT INTO user 
    (userid, name, level, experience, experience_multiplier, ip,point, today_duration, total_duration, last_time)
VALUES 
    (@userid, @name, @level, @experience, @experience_multiplier, @ip,@point, @today_duration, @total_duration, @last_time)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    level = VALUES(level),
    experience = VALUES(experience),
    experience_multiplier = VALUES(experience_multiplier),
    ip = VALUES(ip),
    today_duration = VALUES(today_duration),
    total_duration = VALUES(total_duration),
    last_time = VALUES(last_time);";

                    cmd.CommandText = upsertSql;
                    cmd.Parameters.AddWithValue("@userid", userid);
                    cmd.Parameters.AddWithValue("@name", name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@level", level);
                    cmd.Parameters.AddWithValue("@experience", experience);
                    cmd.Parameters.AddWithValue("@experience_multiplier", experience_multiplier ?? 1.0);
                    cmd.Parameters.AddWithValue("@ip", ip ?? string.Empty);
                    cmd.Parameters.AddWithValue("@point", point);
                    cmd.Parameters.AddWithValue("@today_duration", today_duration ?? TimeSpan.Zero);
                    cmd.Parameters.AddWithValue("@total_duration", total_duration ?? TimeSpan.Zero);
                    cmd.Parameters.AddWithValue("@last_time", last_time ?? DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"更新用户 {userid} 失败: {ex.Message}"); // 记录 SQL 有助于调试
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
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
        public void LogAdminPermission(string userid, string name, int port, string command, string result, string additionalInfo = "", string group = "")
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
        /// 查询指定用户的徽章列表
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns>徽章列表：(player_name, badge, color, expiration_date, is_permanent, notes)</returns>
        public List<(string player_name, string badge, string color, DateTime expiration_date, bool is_permanent, string notes)> QueryBadge(string userid)
        {
            var badges = new List<(string player_name, string badge, string color, DateTime expiration_date, bool is_permanent, string notes)>();

            if (!connected || string.IsNullOrEmpty(userid))
                return badges;

            string query = @"
        SELECT 
            player_name,
            badge,
            color,
            expiration_date,
            is_permanent,
            notes
        FROM badge 
        WHERE userid = @userid 
          AND (is_permanent = 1 OR expiration_date > NOW())
        ORDER BY is_permanent DESC, expiration_date ASC";

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
                            string player_name = reader["player_name"] as string ?? string.Empty;
                            string badgeName = reader["badge"] as string ?? "未知徽章";
                            string color = reader["color"] as string ?? "white";
                            DateTime expiration_date = reader.GetDateTime("expiration_date");
                            bool is_permanent = reader.GetInt32("is_permanent") == 1;
                            string notes = reader["notes"] as string ?? string.Empty;

                            badges.Add((player_name, badgeName, color, expiration_date, is_permanent, notes));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的徽章失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return badges;
        }
        /// <summary>
        /// 更新或插入用户的封禁记录
        /// </summary>
        /// <param name="userid">被封禁者用户ID</param>
        /// <param name="name">被封禁者名称</param>
        /// <param name="issuer_userid">执行者用户ID</param>
        /// <param name="issuer_name">执行者名称</param>
        /// <param name="reason">封禁原因</param>
        /// <param name="start_time">封禁开始时间</param>
        /// <param name="end_time">封禁结束时间</param>
        /// <param name="port">封禁的服务器端口</param>
        /// <returns>是否操作成功</returns>
        /// <summary>
        /// 插入用户的封禁记录（永远插入新记录，不更新）
        /// </summary>
        /// <param name="userid">被封禁者用户ID</param>
        /// <param name="name">被封禁者名称</param>
        /// <param name="issuer_userid">执行者用户ID</param>
        /// <param name="issuer_name">执行者名称</param>
        /// <param name="reason">封禁原因</param>
        /// <param name="start_time">封禁开始时间</param>
        /// <param name="end_time">封禁结束时间</param>
        /// <param name="port">封禁的服务器端口</param>
        /// <returns>是否插入成功</returns>
        public bool InsertBanRecord(
            string userid,
            string name,
            string issuer_userid,
            string issuer_name,
            string reason,
            DateTime start_time,
            DateTime end_time,
            string port)
        {
            if (!connected || string.IsNullOrEmpty(userid))
            {
                Log.Warn("数据库未连接或 userid 为空，无法插入封禁记录。");
                return false;
            }

            string insertSql = @"
        INSERT INTO ban 
        (issuer_name, issuer_userid, name, userid, reason, start_time, end_time, port)
        VALUES 
        (@issuer_name, @issuer_userid, @name, @userid, @reason, @start_time, @end_time, @port)";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(insertSql, connection))
                {
                    cmd.Parameters.AddWithValue("@issuer_name", issuer_name ?? "Unknown");
                    cmd.Parameters.AddWithValue("@issuer_userid", issuer_userid ?? "Unknown");
                    cmd.Parameters.AddWithValue("@name", name ?? "Unknown");
                    cmd.Parameters.AddWithValue("@userid", userid);
                    cmd.Parameters.AddWithValue("@reason", reason ?? "No reason provided");
                    cmd.Parameters.AddWithValue("@start_time", start_time);
                    cmd.Parameters.AddWithValue("@end_time", end_time);
                    cmd.Parameters.AddWithValue("@port", port ?? "Unknown");

                    cmd.ExecuteNonQuery();
                    Log.Info($"✅ 成功插入封禁记录: {userid} 被 {issuer_name} 封禁至 {end_time}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 插入封禁记录失败: {ex.Message}");
                return false;
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }
        public List<(string issuer_name, string issuer_userid, string name, string userid, string reason, DateTime start_time, DateTime end_time, string port)> QueryAllBan(string INuserid)
        {
            var bans = new List<(string, string, string, string, string, DateTime, DateTime, string)>();

            if (!connected)
                return bans;

            string query = @"
SELECT 
            issuer_name,
            issuer_userid,
            name,
            userid,
            reason,
            start_time,
            end_time,
            port
FROM ban
WHERE userid = @userid";

            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(query, connection))
                {
                    // ✅ 先添加参数，再执行
                    cmd.Parameters.AddWithValue("@userid", INuserid);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string issuer_name = reader["issuer_name"] as string ?? "Unknown";
                            string issuer_userid = reader["issuer_userid"] as string ?? "Unknown";
                            string name = reader["name"] as string ?? "Unknown";
                            string userid = reader["userid"] as string ?? "Unknown";
                            string reason = reader["reason"] as string ?? "未提供理由";
                            DateTime start_time = reader.GetDateTime("start_time");
                            DateTime end_time = reader.GetDateTime("end_time");
                            string port = reader["port"] as string ?? "Unknown";

                            bans.Add((issuer_name, issuer_userid, name, userid, reason, start_time, end_time, port));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询所有封禁记录失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return bans;
        }
        public (string issuer_name, string issuer_userid, string name, string userid, string reason, DateTime start_time, DateTime end_time, string port)? QueryBan(string userid)
        {
            if (!connected || string.IsNullOrEmpty(userid))
                return null;

            string query = @"
SELECT 
            issuer_name,
            issuer_userid,
            name,
            userid,
            reason,
            start_time,
            end_time,
            port
FROM ban
WHERE userid = @userid
AND end_time > NOW() 
ORDER BY end_time DESC 
LIMIT 1";

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
                            return (
                                reader["issuer_name"] as string,
                                reader["issuer_userid"] as string,
                                reader["name"] as string,
                                reader["userid"] as string,
                                reader["reason"] as string,
                                reader.GetDateTime("start_time"),
                                reader.GetDateTime("end_time"),
                                reader["port"] as string
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的封禁记录失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return null; // 未找到有效封禁
        }
        /// <summary>
        /// 查询指定用户的管理员权限列表
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns>管理员权限列表：(player_name, port, permissions, expiration_date, is_permanent, notes)</returns>
        public List<(string player_name, string port, string permissions, DateTime expiration_date, bool is_permanent, string notes)> QueryAdmin(string userid)
        {
            var admins = new List<(string, string, string, DateTime, bool, string)>();

            if (!connected || string.IsNullOrEmpty(userid))
                return admins;

            string query = @"
        SELECT 
            player_name,
            port,
            permissions,
            expiration_date,
            is_permanent,
            notes
        FROM admin 
        WHERE userid = @userid 
          AND (is_permanent = 1 OR expiration_date > NOW())
        ORDER BY is_permanent DESC, expiration_date ASC";

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
                            string player_name = reader["player_name"] as string ?? "Unknown";
                            string port = reader["port"] as string ?? "Unknown";
                            string permissions = reader["permissions"] as string ?? "none";
                            DateTime expiration_date = reader.GetDateTime("expiration_date");
                            bool is_permanent = reader.GetBoolean("is_permanent");
                            string notes = reader["notes"] as string ?? string.Empty;

                            admins.Add((player_name, port, permissions, expiration_date, is_permanent, notes));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"❌ 查询用户 {userid} 的管理员权限失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return admins;
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
