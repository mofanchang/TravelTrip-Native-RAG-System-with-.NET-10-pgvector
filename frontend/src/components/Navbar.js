import React from "react";
import { Link, useNavigate } from "react-router-dom";

export default function Navbar() {
  const navigate = useNavigate();
  
  // 從 localStorage 取得狀態
  const token = localStorage.getItem('token');
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    // 使用 navigate('/') 回首頁並強制刷新的小技巧
    window.location.href = "/"; 
  };

  return (
    <nav className="bg-blue-600 text-white p-4 shadow-md sticky top-0 z-50">
      <div className="container mx-auto flex justify-between items-center">
        <Link to="/" className="font-bold text-xl tracking-tighter hover:opacity-80 transition">
          TRAVELTRIP
        </Link>
        
        <div className="flex items-center space-x-6">
          {/* 登入後才看得到的連結 */}
          {token && (
            <>
              <Link to="/cart" className="hover:text-blue-200">🛒 購物車</Link>
              <Link to="/bookings" className="hover:text-blue-200">我的訂單</Link>
              <Link to="/profile" className="hover:text-blue-200">個人資料</Link>
            </>
          )}

          {/* 會員按鈕區 */}
          <div className="border-l border-blue-400 pl-6 flex items-center space-x-4">
            {token ? (
              <>
                <span className="text-sm">你好, <span className="font-bold">{user.name}</span></span>
                <button 
                  onClick={handleLogout} 
                  className="bg-red-500 hover:bg-red-600 px-3 py-1 rounded text-sm transition"
                >
                  登出
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="hover:text-blue-200 text-sm">登入</Link>
                <Link to="/register" className="bg-white text-blue-600 px-4 py-1 rounded font-bold text-sm hover:bg-gray-100 transition">
                  註冊
                </Link>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
}