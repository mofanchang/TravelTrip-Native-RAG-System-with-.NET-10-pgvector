import React from "react";
import { BrowserRouter as Router, Routes, Route, Link } from "react-router-dom";
import ChatWidget from "./ChatWidget";
import TripList from "./components/TripList";
import Cart from "./components/Cart";
import { BookingList, BookingDetail } from "./components/Bookings";
import Profile from "./components/Profile";
import Login from './components/Login';
import Register from './components/Register';
import Navbar from './components/Navbar';
import TripDetail from './components/TripDetail';
import AdminDashboard from './components/AdminDashboard';

export default function App() {
  return (
    <Router>
      <div className="bg-gray-100 min-h-screen font-sans">
        {/* 已切分出去的導覽列 */}
        <Navbar />

        {/* 路由切換區域 */}
        <main className="container mx-auto mt-8 px-4 pb-20">
          <Routes>
            <Route path="/" element={<HomeWithHero />} />
            <Route path="/cart" element={<Cart />} />
            <Route path="/bookings" element={<BookingList />} />
            <Route path="/bookings/:id" element={<BookingDetail />} />
            <Route path="/profile" element={<Profile />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/trips/:id" element={<TripDetail />} />
            <Route path="/admin" element={<AdminDashboard />} />
          </Routes>
        </main>

        <ChatWidget />
      </div>
    </Router>
  );
}

// --- 完整的 HomeWithHero 區塊 ---
function HomeWithHero() {
  // 檢查登入狀態，決定按鈕顯示
  const token = localStorage.getItem('token');

  return (
    <>
      {/* 視覺大圖區 (Hero Section) */}
      <div className="bg-gradient-to-r from-blue-600 to-purple-700 text-white py-20 mb-12 rounded-3xl text-center shadow-xl">
        <h1 className="text-5xl font-extrabold mb-4 tracking-tight">探索世界的美好</h1>
        <p className="text-xl opacity-90 mb-8 font-light">
          專屬 Flora 的 AI 旅遊規劃助手，讓每一趟旅程都與眾不同
        </p>

        {/* 若未登入，顯示註冊/登入引導 */}
        {!token && (
          <div className="flex justify-center space-x-4">
            <Link
              to="/register"
              className="bg-white text-blue-600 px-8 py-3 rounded-full font-bold text-lg hover:bg-gray-100 transition shadow-lg"
            >
              立即開始使用
            </Link>
            <Link
              to="/login"
              className="bg-transparent border-2 border-white text-white px-8 py-3 rounded-full font-bold text-lg hover:bg-white hover:text-blue-600 transition"
            >
              會員登入
            </Link>
          </div>
        )}
      </div>

      {/* 行程列表區塊 */}
      <div className="px-2">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold text-gray-800 border-l-4 border-blue-600 pl-4">
            精選推薦行程
          </h2>
          <button className="text-sm text-blue-600 hover:underline">查看全部</button>
        </div>
        <TripList />
      </div>
    </>
  );
}