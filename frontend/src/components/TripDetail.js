import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import axios from "axios";

const API_BASE = "http://localhost:5000";

export default function TripDetail() {
  const { id } = useParams(); 
  const navigate = useNavigate();
  const [trip, setTrip] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    axios.get(`${API_BASE}/api/Trips/${id}`)
      .then(res => {
        const actualData = res.data.data || res.data;
        setTrip(actualData);
        setLoading(false);
      })
      .catch(err => {
        console.error("抓取失敗", err);
        setLoading(false);
      });
  }, [id]);

  const handleBooking = () => {
    const token = localStorage.getItem('token');
    if (!token) {
      alert("請先登入");
      navigate("/login");
      return;
    }
    alert("正在導向支付頁面...");
    setTimeout(() => navigate("/bookings"), 2000);
  };

  if (loading) return <div className="text-center py-20 text-gray-500">行程載入中...</div>;
  if (!trip) return <div className="text-center py-20">查無此行程</div>;

  return (
    <div className="max-w-6xl mx-auto py-10 px-6">
      <div className="flex flex-col lg:flex-row gap-12">
        
        {/* 左側：圖片與介紹 */}
        <div className="lg:w-2/3">
          <img 
            src={`${API_BASE}${trip.image}`} 
            alt={trip.name} 
            className="w-full h-[450px] object-cover rounded-lg shadow-sm mb-8"
          />
          <h1 className="text-3xl font-bold text-gray-900 mb-2">{trip.name}</h1>
          <p className="text-gray-500 mb-8">{trip.country} / {trip.city}</p>
          
          <div className="border-t pt-8">
            <h2 className="text-xl font-bold mb-4">行程介紹</h2>
            <p className="text-gray-600 leading-relaxed whitespace-pre-line text-lg">
              {trip.description}
            </p>
          </div>
        </div>

        {/* 右側：訂購區塊 */}
        <div className="lg:w-1/3">
          <div className="border border-gray-200 p-8 rounded-lg sticky top-10">
            <div className="mb-6">
              <span className="text-sm text-gray-400">每人起價</span>
              <div className="text-3xl font-bold text-gray-900">TWD {trip.price?.toLocaleString()}</div>
            </div>

            <div className="space-y-4 mb-8">
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">行程天數</span>
                <span className="font-medium text-gray-900">{trip.duration} 天</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">剩餘名額</span>
                <span className="font-medium text-gray-900">{trip.availableSeats} 位</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500">確認方式</span>
                <span className="font-medium text-gray-900">立即確認</span>
              </div>
            </div>

            <button 
              onClick={handleBooking}
              className="w-full bg-black text-white py-4 rounded font-bold hover:bg-gray-800 transition"
            >
              立即預訂
            </button>
            <p className="text-xs text-gray-400 mt-4 text-center">
              * 提前 72 小時可免費取消
            </p>
          </div>
        </div>

      </div>
    </div>
  );
}