import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Login() {
  const [formData, setFormData] = useState({ email: '', password: '' });
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch("http://localhost:5000/api/Auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData)
      });

      const data = await res.json();
      if (res.ok) {
        // 重要：把 Token 和用戶資訊存起來
        localStorage.setItem('token', data.token);
        localStorage.setItem('user', JSON.stringify(data.user));
        
        alert(`歡迎回來，${data.user.name}！`);
        window.location.href = "/"; // 強制重新導向並刷新，讓導覽列更新狀態
      } else {
        alert(data.message || "登入失敗");
      }
    } catch (err) {
      console.error("登入錯誤:", err);
    }
  };

  return (
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow-lg rounded-lg border border-gray-100">
      <h2 className="text-2xl font-bold mb-6 text-center text-blue-600">會員登入</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <input type="email" placeholder="Email" required className="w-full border p-2 rounded" 
               onChange={e => setFormData({...formData, email: e.target.value})} />
        <input type="password" placeholder="密碼" required className="w-full border p-2 rounded" 
               onChange={e => setFormData({...formData, password: e.target.value})} />
        <button className="w-full bg-blue-500 text-white py-2 rounded-lg hover:bg-blue-600 font-semibold transition">
          登入
        </button>
      </form>
    </div>
  );
}