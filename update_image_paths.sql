-- 📊 更新資料庫中的圖片路徑
-- 將 Django 格式轉換為 .NET 格式

-- ================================
-- 檢查目前的圖片路徑格式
-- ================================
SELECT id, name, image 
FROM trips_trip 
LIMIT 10;

-- ================================
-- 備份原始資料（建議先執行）
-- ================================
CREATE TABLE IF NOT EXISTS trips_trip_backup AS 
SELECT * FROM trips_trip;

-- ================================
-- 更新圖片路徑
-- ================================

-- 方案 1：如果 Django 儲存的是完整檔名（如 "tokyo.jpg"）
UPDATE trips_trip 
SET image = '/uploads/trips/' || image 
WHERE image IS NOT NULL 
  AND image != '' 
  AND image NOT LIKE '/uploads/%';

-- 方案 2：如果 Django 儲存的是帶路徑的（如 "trips/tokyo.jpg"）
-- UPDATE trips_trip 
-- SET image = '/uploads/' || image 
-- WHERE image IS NOT NULL 
--   AND image != '' 
--   AND image NOT LIKE '/uploads/%';

-- 方案 3：如果只想更新特定檔案
-- UPDATE trips_trip 
-- SET image = '/uploads/trips/tokyo.jpg' 
-- WHERE name = '東京之旅';

-- ================================
-- 驗證更新結果
-- ================================
SELECT id, name, image 
FROM trips_trip 
WHERE image IS NOT NULL;

-- ================================
-- 如果需要還原（萬一出錯）
-- ================================
-- DELETE FROM trips_trip;
-- INSERT INTO trips_trip SELECT * FROM trips_trip_backup;
-- DROP TABLE trips_trip_backup;

-- ================================
-- 清理備份表（確認無誤後執行）
-- ================================
-- DROP TABLE IF EXISTS trips_trip_backup;
