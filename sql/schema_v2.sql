-- ============================================================
-- UnionPlugin 数据库表结构 v2
-- 表分离: user(核心标识) | user_progression(经验积分时长) | user_snake(小游戏)
-- ============================================================

DROP TABLE IF EXISTS `user_snake`;
DROP TABLE IF EXISTS `user_progression`;
DROP TABLE IF EXISTS `user`;

-- 1. 核心用户表: 身份标识
CREATE TABLE `user` (
  `uid` int NOT NULL AUTO_INCREMENT,
  `userid` char(25) NOT NULL,
  `name` TEXT DEFAULT '',
  `ip` varchar(20) DEFAULT NULL,
  `last_time` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`userid`),
  UNIQUE KEY `uid` (`uid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2. 进度表: 经验/经验倍率/积分/在线时长
CREATE TABLE `user_progression` (
  `userid` char(25) NOT NULL,
  `experience` int NOT NULL DEFAULT 0,
  `experience_multiplier` double DEFAULT 1.0,
  `point` int DEFAULT 0,
  `today_duration` time DEFAULT NULL,
  `total_duration` time DEFAULT NULL,
  PRIMARY KEY (`userid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. 贪吃蛇小游戏表: 最高分/纪录时间
CREATE TABLE `user_snake` (
  `userid` char(25) NOT NULL,
  `name` TEXT DEFAULT '',
  `highscore` int DEFAULT 0,
  `record_time` datetime DEFAULT NULL,
  PRIMARY KEY (`userid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 数据迁移(从旧user表)
INSERT INTO user (uid, userid, name, ip, last_time) SELECT uid, userid, name, ip, last_time FROM old_user;
INSERT INTO user_progression (userid, experience, experience_multiplier, point, today_duration, total_duration) SELECT userid, experience, experience_multiplier, point, today_duration, total_duration FROM old_user;
INSERT INTO user_snake (userid, name, highscore, record_time) SELECT userid, name, highscore, Snack_Create_time FROM old_user WHERE highscore > 0;
