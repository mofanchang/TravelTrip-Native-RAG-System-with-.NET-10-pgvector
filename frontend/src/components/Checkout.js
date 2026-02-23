import React from 'react';
import { useNavigate } from 'react-router-dom';

export default function Checkout() {
  const navigate = useNavigate();

  const handleCheckout = () => {
    fetch("http://localhost:5000/api/Bookings/Create", { method: 'POST' })
      .then(res => res.json())
      .then(data => {
        alert("訂單已建立！");
        navigate(`/bookings/${data.id}`); // 跳轉到訂單詳情
      });
  };

  return (
    <div className="container mx-auto p-6 text-center">
      <h2 className="text-2xl font-bold mb-4">結帳頁面</h2>
      <p className="mb-6 text-gray-600">請確認您要購買的項目，然後完成結帳流程。</p>
      <button onClick={handleCheckout} className="bg-blue-500 text-white px-6 py-2 rounded font-bold hover:bg-blue-600">
        確認並結帳
      </button>
    </div>
  );
}