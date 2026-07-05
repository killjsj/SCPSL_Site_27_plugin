-- ============================================================
-- UnionPlugin 数据库 v3
-- 表: user | player_stats | badge | admin | ban | chat_log | admin_log
-- ============================================================

DROP TABLE IF EXISTS `chat_log`;
DROP TABLE IF EXISTS `user_snake`;
DROP TABLE IF EXISTS `user_progression`;
DROP TABLE IF EXISTS `user`;

-- 1. 用户表: 身份 + 时长 + 经验/积分
CREATE TABLE `user` (
  `uid` int NOT NULL AUTO_INCREMENT,
  `userid` char(25) NOT NULL,
  `name` TEXT DEFAULT NULL,
  `ip` varchar(30) DEFAULT NULL,
  `last_time` datetime DEFAULT CURRENT_TIMESTAMP,
  `today_duration` time DEFAULT '00:00:00',
  `total_duration` time DEFAULT '00:00:00',
  `experience` int DEFAULT 0,
  `experience_multiplier` double DEFAULT 1.0,
  `point` int DEFAULT 0,
  PRIMARY KEY (`userid`),
  UNIQUE KEY `uid` (`uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2. 玩家数据表: 累计击杀/逃离/死亡
CREATE TABLE `player_stats` (
  `userid` char(25) NOT NULL,
  `total_kills` int DEFAULT 0,
  `total_escapes` int DEFAULT 0,
  `total_deaths` int DEFAULT 0,
  PRIMARY KEY (`userid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. 称号颜色表
DROP TABLE IF EXISTS `badge`;
CREATE TABLE `badge` (
  `id` int NOT NULL AUTO_INCREMENT,
  `player_name` TEXT DEFAULT NULL,
  `userid` char(25) DEFAULT NULL,
  `badge` TEXT DEFAULT NULL,
  `color` varchar(10) DEFAULT NULL,
  `expiration_date` datetime NOT NULL,
  `is_permanent` tinyint(1) DEFAULT NULL,
  `notes` TEXT DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 4. 管理表
DROP TABLE IF EXISTS `admin`;
CREATE TABLE `admin` (
  `id` int NOT NULL AUTO_INCREMENT,
  `player_name` TEXT DEFAULT NULL,
  `userid` char(25) NOT NULL,
  `port` varchar(30) DEFAULT NULL,
  `permissions` TEXT DEFAULT NULL,
  `expiration_date` datetime NOT NULL,
  `is_permanent` tinyint(1) DEFAULT NULL,
  `notes` TEXT DEFAULT NULL,
  PRIMARY KEY (`userid`),
  UNIQUE KEY `id` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 5. 封禁表
DROP TABLE IF EXISTS `ban`;
CREATE TABLE `ban` (
  `id` int NOT NULL AUTO_INCREMENT,
  `issuer_name` TEXT DEFAULT NULL,
  `issuer_userid` char(25) DEFAULT NULL,
  `name` TEXT DEFAULT NULL,
  `userid` char(25) DEFAULT NULL,
  `reason` TEXT DEFAULT NULL,
  `start_time` datetime NOT NULL,
  `end_time` datetime NOT NULL,
  `port` varchar(30) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 6. 聊天记录表
CREATE TABLE `chat_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `userid` char(25) DEFAULT NULL,
  `name` TEXT DEFAULT NULL,
  `message` text,
  `channel` varchar(20) DEFAULT NULL,
  `time` datetime DEFAULT CURRENT_TIMESTAMP,
  `port` varchar(30) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 7. 管理操作记录表
DROP TABLE IF EXISTS `admin_log`;
CREATE TABLE `admin_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `userid` varchar(64) DEFAULT NULL,
  `name` TEXT DEFAULT NULL,
  `operation_time` datetime NOT NULL,
  `port` int NOT NULL,
  `command_name` TEXT DEFAULT NULL,
  `command_result` text,
  `additional_info` text,
  `admingroup` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
