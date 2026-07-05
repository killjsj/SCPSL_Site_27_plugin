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
            Log.Info(connectionString);
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
                Log.Error($"❌ 连接数据库失败: {ex}");
            }
        }

        public (int uid, string name, int experience,double? experience_multiplier, int point, string ip, DateTime? last_time, TimeSpan? total_duration, TimeSpan? today_duration) QueryUser(string userid)
        {
            if (!connected)
                return (0, null, 0,0,1,null, null, null, null);

            string query = @"
        SELECT 
            uid,
            name,
            userid,
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
                            int experienceOrdinal = reader.GetOrdinal("experience");
                            int experience_multiplierOrdinal = reader.GetOrdinal("experience_multiplier");
                            int PointOrdinal = reader.GetOrdinal("point");
                            int ipOrdinal = reader.GetOrdinal("ip");
                            int today_durationOrdinal = reader.GetOrdinal("today_duration");
                            int total_durationOrdinal = reader.GetOrdinal("total_duration");
                            int last_timeOrdinal = reader.GetOrdinal("last_time");

                            int uid = reader.IsDBNull(nameOrdinal) ? 0 : reader.GetInt32(uidOrdinal);
                            string name = reader.IsDBNull(nameOrdinal) ? null : reader.GetString(nameOrdinal);
                            int experience = reader.IsDBNull(experienceOrdinal) ? 0 : reader.GetInt32(experienceOrdinal);
                            double? experience_multiplier = reader.IsDBNull(experience_multiplierOrdinal) ? (double?)null : reader.GetDouble(experience_multiplierOrdinal);
                            int point = reader.IsDBNull(PointOrdinal) ? 0: reader.GetInt32(PointOrdinal);
                            string ip = reader.IsDBNull(ipOrdinal) ? "1.1.1.1" : reader.GetString(ipOrdinal);
                            DateTime? last_time = reader.IsDBNull(last_timeOrdinal) ? (DateTime?)null : reader.GetDateTime(last_timeOrdinal);
                            TimeSpan? today_duration = reader.IsDBNull(today_durationOrdinal) ? (TimeSpan?)null : reader.GetTimeSpan(today_durationOrdinal);
                            TimeSpan? total_duration = reader.IsDBNull(total_durationOrdinal) ? (TimeSpan?)null : reader.GetTimeSpan(total_durationOrdinal);

                            return (uid, name, experience, experience_multiplier, point, ip, last_time, total_duration, today_duration);
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
            return (0, null, 0, 0, 1, null, null, null, null);
        }
        //public (string name,int level, int exp) QueryUser(string userid)
        //{
        //    if (!connected)
        //        return (string.Empty, 0, null);

        //    // 从 user 表直接读取 highscore 和 record_time
        //    string query = @"
        //        SELECT 
        //            name, 
        //            highscore, 
        //            record_time 
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
        //                    int timeOrdinal = reader.GetOrdinal("record_time");

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
        public void Update(
            string userid,
            string name = null,
            int experience = -1,
            double? experience_multiplier = null,
            string ip = null,
            int point = -1,
            DateTime? last_time = null,
            TimeSpan? today_duration = null,
            TimeSpan? total_duration = null)
        {
            if (!connected || string.IsNullOrEmpty(userid)) return;

            try
            {
                var p = QueryUser(userid);
                connection.Open();

                name = name ?? p.name;
                point = point == -1 ? p.point : point;
                experience = experience == -1 ? p.experience : experience;
                experience_multiplier = experience_multiplier ?? p.experience_multiplier;
                ip = ip ?? p.ip;
                last_time = last_time ?? p.last_time;
                today_duration = today_duration ?? p.today_duration;
                total_duration = total_duration ?? p.total_duration;

                string sql = @"
INSERT INTO user (userid, name, experience, experience_multiplier, ip, point, today_duration, total_duration, last_time)
VALUES (@userid, @name, @experience, @experience_multiplier, @ip, @point, @today_duration, @total_duration, @last_time)
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    experience = VALUES(experience),
    experience_multiplier = VALUES(experience_multiplier),
    ip = VALUES(ip),
    point = VALUES(point),
    today_duration = VALUES(today_duration),
    total_duration = VALUES(total_duration),
    last_time = VALUES(last_time);";
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    cmd.Parameters.AddWithValue("@name", name ?? string.Empty);
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
                Log.Error($"更新用户 {userid} 失败: {ex.Message}");
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// 插入聊天记录
        /// </summary>
        public void InsertChatLog(string userid, string name, string message, string channel, string port)
        {
            if (!connected || string.IsNullOrEmpty(userid)) return;
            string sql = @"
INSERT INTO chat_log (userid, name, message, channel, time, port)
VALUES (@userid, @name, @message, @channel, @time, @port);";
            try
            {
                connection.Open();
                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@userid", userid);
                    cmd.Parameters.AddWithValue("@name", name ?? "");
                    cmd.Parameters.AddWithValue("@message", message ?? "");
                    cmd.Parameters.AddWithValue("@channel", channel ?? "");
                    cmd.Parameters.AddWithValue("@time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@port", port ?? "");
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Log.Error($"插入聊天记录失败: {ex.Message}"); }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// 查询用户在所有服务器的违规总次数（ban表记录数）
        /// </summary>
        public int CountUserViolations(string userid)
        {
            if (!connected || string.IsNullOrEmpty(userid))
                return 0;
            return QueryAllBan(userid).Count;
        }

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
        /// <summary>
        /// 记录管理员操作日志
        /// </summary>
        public void LogAdminPermission(string userid, string name, int port, string command, string result, string additionalInfo = "", string group = "")
        {
            if (!connected) return;

            string query = @"
                INSERT INTO admin_log 
                (userid, name, operation_time, port, command_name, command_result, additional_info,admingroup)
                VALUES 
                (@userid, @name, @operation_time, @port, @command_name, @command_result, @additional_info,@admingroup)";

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
                    cmd.Parameters.AddWithValue("@admingroup", group ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Log.Error($"❌ 记录管理员权限日志失败: {ex.Message}"); }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        /// <summary>
        /// 查询用户称号
        /// </summary>
        public List<(string player_name, string badge, string color, DateTime expiration_date, bool is_permanent, string notes)> QueryBadge(string userid)
        {
            var badges = new List<(string player_name, string badge, string color, DateTime expiration_date, bool is_permanent, string notes)>();
            if (!connected || string.IsNullOrEmpty(userid)) return badges;

            string query = @"
        SELECT player_name, badge, color, expiration_date, is_permanent, notes
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
                            string badge = reader["badge"] as string ?? string.Empty;
                            string color = reader["color"] as string ?? "white";
                            DateTime expiration_date = reader.GetDateTime("expiration_date");
                            bool is_permanent = Convert.ToBoolean(reader["is_permanent"]);
                            string notes = reader["notes"] as string ?? string.Empty;
                            badges.Add((player_name, badge, color, expiration_date, is_permanent, notes));
                        }
                    }
                }
            }
            catch (Exception ex) { Log.Error($"❌ 查询称号失败: {ex.Message}"); }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
            return badges;
        }

        public void Close()
        {
            connection?.Close();
        }

        ~MySQLConnect()
        {
            Close();
        }

    }
}