import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';

export default function Cart() {
  const [trips, setTrips] = useState([]);
  const [loading, setLoading] = useState(true);

  // 1. 取得購物車內容 (fetchCart)
  const fetchCart = async () => {
    try {
      const token = localStorage.getItem('token');
      if (!token) {
        console.warn("未登入，無法取得購物車");
        setLoading(false);
        return;
      }

      const res = await fetch("http://localhost:5000/api/Cart", {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (!res.ok) throw new Error(`取得失敗: ${res.status}`);

      const data = await res.json();
      // 注意：根據妳後端 DTO，如果是回傳 { items: [...] } 則用 data.items
      setTrips(data.items || data);
    } catch (err) {
      console.error("購物車載入失敗:", err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCart();
  }, []);

  // 2. 刪除單項 (removeItem)
  const removeItem = async (id) => {
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`http://localhost:5000/api/Cart/Remove/${id}`, { 
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` } // 也要帶 Token
      });
      
      if (res.ok) {
        fetchCart(); // 刪除成功後重新整理列表
      } else {
        alert("刪除失敗，請檢查權限");
      }
    } catch (err) {
      console.error("刪除失敗:", err);
    }
  };

  // 3. 清空購物車 (clearCart)
  const clearCart = async () => {
    if (!window.confirm("確定要清空購物車嗎？")) return;
    
    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`http://localhost:5000/api/Cart/Clear`, { 
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` } // 也要帶 Token
      });

      if (res.ok) {
        setTrips([]);
      }
    } catch (err) {
      console.error("清空失敗:", err);
    }
  };

  if (loading) return <div className="p-10 text-center">讀取中...</div>;

  return (
    <div className="container mx-auto p-6">
      <h2 className="text-2xl font-bold mb-4">購物車</h2>
      {trips && trips.length > 0 ? (
        <>
          <ul className="bg-white shadow rounded-lg p-4">
            {trips.map(item => (
              <li key={item.id} className="mb-2 border-b pb-2 flex justify-between items-center">
                <span><strong>{item.tripName || item.name}</strong> - ${item.price}</span>
                <button 
                  onClick={() => removeItem(item.id)} 
                  className="bg-red-100 text-red-600 px-3 py-1 rounded hover:bg-red-200 transition"
                >
                  刪除
                </button>
              </li>
            ))}
          </ul>
          <div className="mt-6 space-x-4">
            <Link to="/checkout" className="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600 inline-block transition">
              前往結帳
            </Link>
            <button 
              onClick={clearCart} 
              className="bg-gray-200 text-gray-700 px-4 py-2 rounded hover:bg-gray-300 transition"
            >
              清空購物車
            </button>
          </div>
        </>
      ) : (
        <div className="text-center py-10 bg-gray-50 rounded-lg border-2 border-dashed border-gray-200">
          <p className="text-gray-500">購物車是空的，去逛逛行程吧！</p>
          <Link to="/" className="text-blue-500 underline mt-2 inline-block">回首頁</Link>
        </div>
      )}
    </div>
  );
}