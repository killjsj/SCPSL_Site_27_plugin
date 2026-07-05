-- ============================================================
-- UnionPlugin v2 → v3 迁移 (user+user_progression → user 合并)
-- 当前状态: user(基础) | user_progression(进度) | user_snake(蛇)
-- 目标状态: user(全部合并) | player_stats | chat_log
-- 运行前请备份数据库
-- ============================================================

-- 1. 合并 user_progression 到 user: 先加列
ALTER TABLE `user` ADD COLUMN `experience` int DEFAULT 0 AFTER `last_time`;
ALTER TABLE `user` ADD COLUMN `experience_multiplier` double DEFAULT 1.0 AFTER `experience`;
ALTER TABLE `user` ADD COLUMN `point` int DEFAULT 0 AFTER `experience_multiplier`;
ALTER TABLE `user` ADD COLUMN `today_duration` time DEFAULT '00:00:00' AFTER `point`;
ALTER TABLE `user` ADD COLUMN `total_duration` time DEFAULT '00:00:00' AFTER `today_duration`;

-- 2. 从 user_progression 迁移数据到 user
UPDATE `user` u
INNER JOIN `user_progression` p ON u.userid = p.userid
SET
    u.experience = p.experience,
    u.experience_multiplier = COALESCE(p.experience_multiplier, 1.0),
    u.point = COALESCE(p.point, 0),
    u.today_duration = COALESCE(p.today_duration, '00:00:00'),
    u.total_duration = COALESCE(p.total_duration, '00:00:00');

-- 3. 扩展文本列宽度
ALTER TABLE `user` MODIFY `name` TEXT DEFAULT NULL;
ALTER TABLE `user` MODIFY `ip` varchar(30) DEFAULT NULL;
ALTER TABLE `user` MODIFY `last_time` datetime DEFAULT CURRENT_TIMESTAMP;

-- 4. 新建 player_stats
CREATE TABLE IF NOT EXISTS `player_stats` (
  `userid` char(25) NOT NULL,
  `total_kills` int DEFAULT 0,
  `total_escapes` int DEFAULT 0,
  `total_deaths` int DEFAULT 0,
  PRIMARY KEY (`userid`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 5. 扩展 badge / admin / ban 文本列
ALTER TABLE `badge` MODIFY `player_name` TEXT DEFAULT NULL;
ALTER TABLE `badge` MODIFY `badge` TEXT DEFAULT NULL;
ALTER TABLE `badge` MODIFY `notes` TEXT DEFAULT NULL;

ALTER TABLE `admin` MODIFY `player_name` TEXT DEFAULT NULL;
ALTER TABLE `admin` MODIFY `permissions` TEXT DEFAULT NULL;
ALTER TABLE `admin` MODIFY `notes` TEXT DEFAULT NULL;

ALTER TABLE `ban` MODIFY `issuer_name` TEXT DEFAULT NULL;
ALTER TABLE `ban` MODIFY `name` TEXT DEFAULT NULL;
ALTER TABLE `ban` MODIFY `reason` TEXT DEFAULT NULL;

-- 6. 新建 chat_log
CREATE TABLE IF NOT EXISTS `chat_log` (
  `id` int NOT NULL AUTO_INCREMENT,
  `userid` char(25) DEFAULT NULL,
  `name` TEXT DEFAULT NULL,
  `message` text,
  `channel` varchar(20) DEFAULT NULL,
  `time` datetime DEFAULT CURRENT_TIMESTAMP,
  `port` varchar(30) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 7. 修正 admin_log
ALTER TABLE `admin_log` MODIFY `name` TEXT DEFAULT NULL;
ALTER TABLE `admin_log` MODIFY `command_name` TEXT DEFAULT NULL;
ALTER TABLE `admin_log` CHANGE COLUMN `admingrooup` `admingroup` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- 8. 验证后删除废弃表
-- SELECT COUNT(*) FROM user WHERE experience = 0;
DROP TABLE IF EXISTS `user_progression`;
DROP TABLE IF EXISTS `user_snake`;
