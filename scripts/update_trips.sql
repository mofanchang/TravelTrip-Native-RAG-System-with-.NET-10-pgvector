-- 步驟 1：備份原始資料
CREATE TABLE trips_trip_backup AS SELECT * FROM trips_trip;

-- 步驟 2：更新京都行程的圖片
UPDATE trips_trip 
SET image = '/uploads/trips/japan-1096822_640.jpg' 
WHERE id = 1 AND name = '京都賞櫻之旅';

-- 步驟 3：更新巴黎行程的圖片
UPDATE trips_trip 
SET image = '/uploads/trips/bridge-547284_640.jpg' 
WHERE id = 2 AND name = '巴黎藝術饗宴';

-- 步驟 4：驗證更新結果
SELECT id, name, image, country FROM trips_trip;

-- 步驟 5：確認備份
SELECT COUNT(*) as backup_count FROM trips_trip_backup;
