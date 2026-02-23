#!/usr/bin/env python3
"""
Django Media 檔案遷移到 .NET wwwroot
將 Django 的 media/ 資料夾複製到 .NET 的 wwwroot/uploads/
"""

import os
import shutil
from pathlib import Path

# 路徑設定
DJANGO_MEDIA_PATH = "/Users/Flora/Documents/TRAVELTRIP_PROJECT/media"
DOTNET_UPLOADS_PATH = "/Users/Flora/Documents/TRAVELTRIP_NET_VERSION/TravelTripApi/wwwroot/uploads/trips"

def migrate_media_files():
    """遷移所有 media 檔案"""
    
    if not os.path.exists(DJANGO_MEDIA_PATH):
        print(f"❌ Django media 路徑不存在: {DJANGO_MEDIA_PATH}")
        return
    
    # 建立目標目錄
    os.makedirs(DOTNET_UPLOADS_PATH, exist_ok=True)
    
    # 統計
    total_files = 0
    copied_files = 0
    skipped_files = 0
    
    print("🚀 開始遷移 Media 檔案...")
    print(f"來源: {DJANGO_MEDIA_PATH}")
    print(f"目標: {DOTNET_UPLOADS_PATH}")
    print()
    
    # 遍歷所有檔案
    for filename in os.listdir(DJANGO_MEDIA_PATH):
        source_file = os.path.join(DJANGO_MEDIA_PATH, filename)
        
        # 跳過目錄
        if os.path.isdir(source_file):
            continue
        
        total_files += 1
        
        # 檢查檔案類型
        ext = os.path.splitext(filename)[1].lower()
        if ext not in ['.jpg', '.jpeg', '.png', '.gif', '.webp']:
            print(f"⏭️  跳過非圖片檔案: {filename}")
            skipped_files += 1
            continue
        
        # 複製檔案
        destination_file = os.path.join(DOTNET_UPLOADS_PATH, filename)
        
        try:
            shutil.copy2(source_file, destination_file)
            print(f"✅ 複製: {filename}")
            copied_files += 1
        except Exception as e:
            print(f"❌ 複製失敗 {filename}: {e}")
    
    print()
    print("=" * 50)
    print(f"📊 遷移完成統計:")
    print(f"   總檔案數: {total_files}")
    print(f"   已複製: {copied_files}")
    print(f"   已跳過: {skipped_files}")
    print("=" * 50)

def update_database_image_paths():
    """
    更新資料庫中的圖片路徑
    Django: /media/image.jpg
    .NET:   /uploads/trips/image.jpg
    """
    print()
    print("⚠️  注意: 需要手動更新資料庫中的圖片路徑")
    print()
    print("SQL 更新語法:")
    print("""
    -- PostgreSQL
    UPDATE trips_trip 
    SET image = REPLACE(image, '/media/', '/uploads/trips/')
    WHERE image LIKE '/media/%';
    
    -- 或者只儲存檔名
    UPDATE trips_trip 
    SET image = SUBSTRING(image FROM '[^/]+$')
    WHERE image LIKE '/%';
    """)

if __name__ == "__main__":
    migrate_media_files()
    update_database_image_paths()
