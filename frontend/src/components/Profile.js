import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Profile() {
  const [profile, setProfile] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchProfile = async () => {
      // 1. 從口袋 (localStorage) 拿出房卡 (Token)
      const token = localStorage.getItem('token');
      
      if (!token) {
        setError("請先登入");
        return;
      }

      try {
        // 2. 刷卡進入後端受保護的 API
        const response = await axios.get(`http://localhost:5000/api/user/profile`, {
          headers: {
            Authorization: `Bearer ${token}` // 這是最專業的寫法
          }
        });
        setProfile(response.data);
      } catch (err) {
        setError("登入已過期，請重新登入");
        localStorage.removeItem('token'); // 清除無效 Token
      }
    };

    fetchProfile();
  }, []);

  if (error) return <div className="text-red-500 text-center mt-10">{error}</div>;
  if (!profile) return <div className="text-center mt-10">載入中...</div>;

  return (
    <div className="max-w-md mx-auto mt-10 p-6 bg-white rounded-2xl shadow-lg border border-gray-100">
      <h2 className="text-2xl font-bold mb-6 text-blue-600">個人資料</h2>
      <div className="space-y-4">
        <p className="flex justify-between border-b pb-2">
            <span className="text-gray-500">Email</span>
            <span className="font-medium">{profile.email}</span>
        </p>
        <p className="flex justify-between border-b pb-2">
            <span className="text-gray-500">用戶名稱</span>
            <span className="font-medium">{profile.name}</span>
        </p>
      </div>
    </div>
  );
}