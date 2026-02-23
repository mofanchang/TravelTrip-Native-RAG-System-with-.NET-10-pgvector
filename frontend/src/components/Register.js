import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';

export default function Register() {
  const [formData, setFormData] = useState({ name: '', email: '', password: '' });
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch("http://localhost:5000/api/Auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData)
      });

      const data = await res.json();
      if (res.ok) {
        alert("註冊成功！請登入");
        navigate("/login");
      } else {
        alert(data.message || "註冊失敗");
      }
    } catch (err) {
      console.error("註冊錯誤:", err);
    }
  };

  return (
    <div className="max-w-md mx-auto mt-10 p-6 bg-white shadow-lg rounded-lg border border-gray-100">
      <h2 className="text-2xl font-bold mb-6 text-center text-blue-600">加入會員</h2>
      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">姓名</label>
          <input type="text" required className="w-full border p-2 rounded mt-1 focus:ring-2 focus:ring-blue-400 outline-none" 
                 onChange={e => setFormData({...formData, name: e.target.value})} />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Email</label>
          <input type="email" required className="w-full border p-2 rounded mt-1 focus:ring-2 focus:ring-blue-400 outline-none" 
                 onChange={e => setFormData({...formData, email: e.target.value})} />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">密碼</label>
          <input type="password" required className="w-full border p-2 rounded mt-1 focus:ring-2 focus:ring-blue-400 outline-none" 
                 onChange={e => setFormData({...formData, password: e.target.value})} />
        </div>
        <button className="w-full bg-blue-500 text-white py-2 rounded-lg hover:bg-blue-600 transition duration-200 font-semibold">
          立即註冊
        </button>
      </form>
    </div>
  );
}