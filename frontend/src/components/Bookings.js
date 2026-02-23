import React, { useState, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';

// 訂單列表
export function BookingList() {
  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch("http://localhost:5000/api/Bookings/my-bookings", {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
      .then(res => {
        if (!res.ok) throw new Error("無法取得訂單，請檢查登入狀態或後端 [AllowAnonymous] 設定");
        return res.json();
      })
      .then(data => {
        // 關鍵點：你的 .NET 回傳可能是 { items: [...], totalCount: 10 }
        // 所以要判斷是用 data.items 還是 data
        const list = data.items ? data.items : (Array.isArray(data) ? data : []);
        setBookings(list);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  if (loading) return <div className="text-center p-10">讀取訂單中...</div>;

  return (
    <div className="container mx-auto p-6">
      <h2 className="text-2xl font-bold mb-4">我的訂單</h2>
      {bookings.length > 0 ? (
        <ul className="space-y-4">
          {bookings.map(b => (
            <li key={b.id} className="border p-4 rounded shadow bg-white hover:bg-gray-50 transition">
              <p><strong>訂單編號：</strong> {b.id}</p>
              <p><strong>總金額：</strong> ${b.totalAmount || b.total}</p> {/* 檢查你的 DTO 欄位名 */}
              <p><strong>狀態：</strong> <span className="text-orange-600">{b.status}</span></p>
              <Link to={`/bookings/${b.id}`} className="text-blue-600 underline mt-2 inline-block">查看詳情</Link>
            </li>
          ))}
        </ul>
      ) : (
        <div className="bg-white p-10 rounded shadow text-center text-gray-500">
          你目前沒有任何訂單。
        </div>
      )}
    </div>
  );
}

// 訂單詳情
export function BookingDetail() {
  const { id } = useParams();
  const [booking, setBooking] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetch(`http://localhost:5000/api/Bookings/${id}`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })
      .then(res => {
        if (!res.ok) throw new Error("找不到該訂單");
        return res.json();
      })
      .then(setBooking)
      .catch(err => setError(err.message));
  }, [id]);

  if (error) return <div className="p-10 text-red-500 text-center">{error}</div>;
  if (!booking) return <div className="p-10 text-center">載入中...</div>;

  return (
    <div className="container mx-auto p-6 max-w-2xl">
      <h2 className="text-2xl font-bold mb-4">訂單詳情</h2>
      <div className="border p-6 rounded-lg shadow-lg bg-white">
        <div className="grid grid-cols-2 gap-4">
          <p className="font-bold">訂單編號：</p><p>{booking.id}</p>
          <p className="font-bold">總金額：</p><p className="text-blue-600 text-xl font-bold">${booking.totalAmount || booking.total}</p>
          <p className="font-bold">狀態：</p><p>{booking.status}</p>
          <p className="font-bold">建立日期：</p><p>{new Date(booking.createdAt).toLocaleString()}</p>
        </div>
        
        {/* 如果有訂單項目，可以在這裡 map 內容 */}
        {booking.items && (
          <div className="mt-6 border-t pt-4">
            <h3 className="font-bold mb-2">訂購行程：</h3>
            {booking.items.map(item => (
              <div key={item.id} className="text-sm text-gray-600">{item.tripName} x {item.quantity}</div>
            ))}
          </div>
        )}
      </div>
      <Link to="/bookings" className="text-blue-600 underline mt-6 inline-block">← 回到訂單列表</Link>
    </div>
  );
}