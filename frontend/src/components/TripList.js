import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";

const API_BASE = "http://localhost:5000";

export default function TripList() {
  const [trips, setTrips] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch(`${API_BASE}/api/Trips`)
      .then((res) => res.json())
      .then((result) => {
        // 修正點：.NET 回傳的是 { count: 31, data: [...] }
        const actualData = result.data || result;
        setTrips(Array.isArray(actualData) ? actualData : []);
        setLoading(false);
      })
      .catch((err) => {
        console.error("抓取行程失敗:", err);
        setLoading(false);
      });
  }, []);

  if (loading) return <div className="text-center py-10 text-blue-600 font-semibold">載入精選行程中...</div>;

  return (
    <section className="mb-12">
      <h2 className="text-3xl font-bold text-center mb-8 text-gray-800">熱門目的地</h2>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        {trips.length > 0 ? (
          trips.map((trip) => (
            <div key={trip.id} className="bg-white rounded-lg shadow-md hover:shadow-xl transition-all duration-300">
              <div className="h-48 overflow-hidden rounded-t-lg">
                <img 
                  // 修正點：確保圖片路徑補上 http://localhost:5000
                  src={trip.image ? `${API_BASE}${trip.image}` : "https://via.placeholder.com/400x300?text=No+Image"} 
                  alt={trip.name} 
                  className="w-full h-full object-cover hover:scale-110 transition duration-500"
                />
              </div>
              <div className="p-5">
                <h3 className="text-xl font-bold mb-2 text-gray-800">{trip.name}</h3>
                <p className="text-gray-500 text-sm mb-2">{trip.city}, {trip.country}</p>
                <p className="text-gray-600 mb-4 line-clamp-2 h-12">{trip.description}</p>
                <div className="flex justify-between items-center border-t pt-4">
                  <span className="text-2xl font-bold text-blue-600">NT$ {trip.price?.toLocaleString()}</span>
                  <Link 
                    to={`/trips/${trip.id}`} 
                    className="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2 rounded-full font-medium transition"
                  >
                    查看詳情
                  </Link>
                </div>
              </div>
            </div>
          ))
        ) : (
          <div className="col-span-3 text-center text-gray-500 py-20 bg-gray-50 rounded-xl border-2 border-dashed">
            目前沒有可用行程。
          </div>
        )}
      </div>
    </section>
  );
}