import React, { useState, useEffect } from 'react';
import axios from 'axios';

const API_URL = 'http://localhost:5000';

export default function AdminDashboard() {
  const [activeTab, setActiveTab] = useState('trips'); // trips, users, stats
  const [trips, setTrips] = useState([]);
  const [users, setUsers] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);
  const [editingTrip, setEditingTrip] = useState(null);
  const [newTrip, setNewTrip] = useState({
    name: '',
    description: '',
    price: '',
    image: '',
    startDate: '',
    endDate: '',
    availableSeats: '',
    location: '',
    duration: '',
    country: '',
    city: '',
    keywords: ''
  });

  // 取得行程列表
  const fetchTrips = async () => {
    try {
      setLoading(true);
      const res = await axios.get(`${API_URL}/api/admin/trips`);
      setTrips(res.data.data);
    } catch (err) {
      console.error('取得行程失敗:', err.response?.status, err.response?.data);
      alert('取得行程失敗: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // 取得使用者列表
  const fetchUsers = async () => {
    try {
      setLoading(true);
      const res = await axios.get(`${API_URL}/api/admin/users`);
      setUsers(res.data.data);
    } catch (err) {
      console.error('取得使用者失敗:', err.response?.status, err.response?.data);
      alert('取得使用者失敗: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // 取得統計資訊
  const fetchStats = async () => {
    try {
      setLoading(true);
      const res = await axios.get(`${API_URL}/api/admin/stats`);
      setStats(res.data.data);
    } catch (err) {
      console.error('取得統計資訊失敗:', err.response?.status, err.response?.data);
      alert('取得統計資訊失敗: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'trips') fetchTrips();
    else if (activeTab === 'users') fetchUsers();
    else if (activeTab === 'stats') fetchStats();
  }, [activeTab]);

  // 建立新行程
  const handleCreateTrip = async (e) => {
    e.preventDefault();
    try {
      await axios.post(`${API_URL}/api/admin/trips`, newTrip);
      alert('行程建立成功');
      setNewTrip({
        name: '', description: '', price: '', image: '', 
        startDate: '', endDate: '', availableSeats: '', 
        location: '', duration: '', country: '', city: '', keywords: ''
      });
      fetchTrips();
    } catch (err) {
      console.error('建立行程失敗:', err.response?.status, err.response?.data);
      alert('建立行程失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  // 更新行程
  const handleUpdateTrip = async (e) => {
    e.preventDefault();
    try {
      await axios.put(`${API_URL}/api/admin/trips/${editingTrip.id}`, editingTrip);
      alert('行程更新成功');
      setEditingTrip(null);
      fetchTrips();
    } catch (err) {
      console.error('更新行程失敗:', err.response?.status, err.response?.data);
      alert('更新行程失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  // 刪除行程
  const handleDeleteTrip = async (id) => {
    if (!window.confirm('確定要刪除這個行程嗎？')) return;
    try {
      await axios.delete(`${API_URL}/api/admin/trips/${id}`);
      alert('行程刪除成功');
      fetchTrips();
    } catch (err) {
      console.error('刪除行程失敗:', err.response?.status, err.response?.data);
      alert('刪除行程失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  // 切換使用者狀態
  const handleToggleUserActive = async (id) => {
    try {
      await axios.patch(`${API_URL}/api/admin/users/${id}/toggle-active`, {});
      fetchUsers();
    } catch (err) {
      console.error('切換使用者狀態失敗:', err.response?.status, err.response?.data);
      alert('切換使用者狀態失敗: ' + (err.response?.data?.message || err.message));
    }
  };

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-blue-600 text-white p-4">
        <h1 className="text-2xl font-bold">管理後台</h1>
      </nav>

      <div className="flex">
        {/* 側邊欄 */}
        <div className="w-48 bg-white shadow-lg p-4 min-h-screen">
          <button
            onClick={() => setActiveTab('trips')}
            className={`w-full text-left px-4 py-2 rounded mb-2 ${
              activeTab === 'trips' ? 'bg-blue-600 text-white' : 'hover:bg-gray-100'
            }`}
          >
            🧳 行程管理
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`w-full text-left px-4 py-2 rounded mb-2 ${
              activeTab === 'users' ? 'bg-blue-600 text-white' : 'hover:bg-gray-100'
            }`}
          >
            👥 使用者管理
          </button>
          <button
            onClick={() => setActiveTab('stats')}
            className={`w-full text-left px-4 py-2 rounded mb-2 ${
              activeTab === 'stats' ? 'bg-blue-600 text-white' : 'hover:bg-gray-100'
            }`}
          >
            📊 統計資訊
          </button>
        </div>

        {/* 主內容區 */}
        <div className="flex-1 p-8">
          {loading ? (
            <div className="text-center py-20 text-xl">載入中...</div>
          ) : (
            <>
              {/* 行程管理 */}
              {activeTab === 'trips' && (
                <div>
                  <h2 className="text-3xl font-bold mb-6">行程管理</h2>

                  {/* 新增行程表單 */}
                  <div className="bg-white p-6 rounded-lg shadow mb-6">
                    <h3 className="text-xl font-bold mb-4">新增行程</h3>
                    <form onSubmit={handleCreateTrip} className="grid grid-cols-2 gap-4">
                      <input
                        type="text"
                        placeholder="行程名稱"
                        value={newTrip.name}
                        onChange={(e) => setNewTrip({ ...newTrip, name: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <input
                        type="number"
                        placeholder="價格"
                        value={newTrip.price}
                        onChange={(e) => setNewTrip({ ...newTrip, price: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <input
                        type="text"
                        placeholder="國家"
                        value={newTrip.country}
                        onChange={(e) => setNewTrip({ ...newTrip, country: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <input
                        type="text"
                        placeholder="城市"
                        value={newTrip.city}
                        onChange={(e) => setNewTrip({ ...newTrip, city: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <input
                        type="text"
                        placeholder="圖片路徑 (如: /uploads/trips/xxx.jpg)"
                        value={newTrip.image}
                        onChange={(e) => setNewTrip({ ...newTrip, image: e.target.value })}
                        className="border rounded px-3 py-2 col-span-2"
                      />
                      <textarea
                        placeholder="描述"
                        value={newTrip.description}
                        onChange={(e) => setNewTrip({ ...newTrip, description: e.target.value })}
                        className="border rounded px-3 py-2 col-span-2"
                        required
                      />
                      <input
                        type="number"
                        placeholder="可用座位數"
                        value={newTrip.availableSeats}
                        onChange={(e) => setNewTrip({ ...newTrip, availableSeats: e.target.value })}
                        className="border rounded px-3 py-2"
                      />
                      <input
                        type="number"
                        placeholder="天數"
                        value={newTrip.duration}
                        onChange={(e) => setNewTrip({ ...newTrip, duration: e.target.value })}
                        className="border rounded px-3 py-2"
                      />
                      <input
                        type="datetime-local"
                        placeholder="開始日期"
                        value={newTrip.startDate}
                        onChange={(e) => setNewTrip({ ...newTrip, startDate: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <input
                        type="datetime-local"
                        placeholder="結束日期"
                        value={newTrip.endDate}
                        onChange={(e) => setNewTrip({ ...newTrip, endDate: e.target.value })}
                        className="border rounded px-3 py-2"
                        required
                      />
                      <button type="submit" className="col-span-2 bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
                        新增行程
                      </button>
                    </form>
                  </div>

                  {/* 行程列表 */}
                  <div className="bg-white rounded-lg shadow overflow-hidden">
                    <table className="w-full">
                      <thead className="bg-gray-200">
                        <tr>
                          <th className="px-4 py-2 text-left">ID</th>
                          <th className="px-4 py-2 text-left">名稱</th>
                          <th className="px-4 py-2 text-left">國家</th>
                          <th className="px-4 py-2 text-left">城市</th>
                          <th className="px-4 py-2 text-left">價格</th>
                          <th className="px-4 py-2 text-left">座位</th>
                          <th className="px-4 py-2 text-left">操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {trips.map((trip) => (
                          <tr key={trip.id} className="border-b hover:bg-gray-50">
                            <td className="px-4 py-2">{trip.id}</td>
                            <td className="px-4 py-2">{trip.name}</td>
                            <td className="px-4 py-2">{trip.country}</td>
                            <td className="px-4 py-2">{trip.city}</td>
                            <td className="px-4 py-2">NT${trip.price}</td>
                            <td className="px-4 py-2">{trip.availableSeats}</td>
                            <td className="px-4 py-2 space-x-2">
                              <button
                                onClick={() => setEditingTrip(trip)}
                                className="bg-yellow-500 text-white px-2 py-1 rounded hover:bg-yellow-600 text-sm"
                              >
                                編輯
                              </button>
                              <button
                                onClick={() => handleDeleteTrip(trip.id)}
                                className="bg-red-500 text-white px-2 py-1 rounded hover:bg-red-600 text-sm"
                              >
                                刪除
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {/* 編輯行程模態 */}
                  {editingTrip && (
                    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                      <div className="bg-white p-8 rounded-lg max-w-2xl max-h-screen overflow-y-auto">
                        <h3 className="text-2xl font-bold mb-4">編輯行程</h3>
                        <form onSubmit={handleUpdateTrip} className="grid grid-cols-2 gap-4">
                          <input
                            type="text"
                            value={editingTrip.name}
                            onChange={(e) => setEditingTrip({ ...editingTrip, name: e.target.value })}
                            className="border rounded px-3 py-2"
                          />
                          <input
                            type="number"
                            value={editingTrip.price}
                            onChange={(e) => setEditingTrip({ ...editingTrip, price: parseFloat(e.target.value) })}
                            className="border rounded px-3 py-2"
                          />
                          <input
                            type="text"
                            value={editingTrip.country}
                            onChange={(e) => setEditingTrip({ ...editingTrip, country: e.target.value })}
                            className="border rounded px-3 py-2"
                          />
                          <input
                            type="text"
                            value={editingTrip.city}
                            onChange={(e) => setEditingTrip({ ...editingTrip, city: e.target.value })}
                            className="border rounded px-3 py-2"
                          />
                          <textarea
                            value={editingTrip.description}
                            onChange={(e) => setEditingTrip({ ...editingTrip, description: e.target.value })}
                            className="border rounded px-3 py-2 col-span-2"
                          />
                          <input
                            type="number"
                            value={editingTrip.availableSeats}
                            onChange={(e) => setEditingTrip({ ...editingTrip, availableSeats: parseInt(e.target.value) })}
                            className="border rounded px-3 py-2"
                          />
                          <input
                            type="number"
                            value={editingTrip.duration}
                            onChange={(e) => setEditingTrip({ ...editingTrip, duration: parseInt(e.target.value) })}
                            className="border rounded px-3 py-2"
                          />
                          <div className="col-span-2 space-x-4">
                            <button type="submit" className="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700">
                              保存
                            </button>
                            <button
                              type="button"
                              onClick={() => setEditingTrip(null)}
                              className="bg-gray-600 text-white px-4 py-2 rounded hover:bg-gray-700"
                            >
                              取消
                            </button>
                          </div>
                        </form>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {/* 使用者管理 */}
              {activeTab === 'users' && (
                <div>
                  <h2 className="text-3xl font-bold mb-6">使用者管理</h2>
                  <div className="bg-white rounded-lg shadow overflow-hidden">
                    <table className="w-full">
                      <thead className="bg-gray-200">
                        <tr>
                          <th className="px-4 py-2 text-left">ID</th>
                          <th className="px-4 py-2 text-left">Email</th>
                          <th className="px-4 py-2 text-left">名稱</th>
                          <th className="px-4 py-2 text-left">預訂數</th>
                          <th className="px-4 py-2 text-left">購物車</th>
                          <th className="px-4 py-2 text-left">狀態</th>
                          <th className="px-4 py-2 text-left">操作</th>
                        </tr>
                      </thead>
                      <tbody>
                        {users.map((user) => (
                          <tr key={user.id} className="border-b hover:bg-gray-50">
                            <td className="px-4 py-2">{user.id}</td>
                            <td className="px-4 py-2">{user.email}</td>
                            <td className="px-4 py-2">{user.firstName} {user.lastName}</td>
                            <td className="px-4 py-2">{user.bookingCount}</td>
                            <td className="px-4 py-2">{user.cartItems}</td>
                            <td className="px-4 py-2">
                              <span className={`px-2 py-1 rounded text-sm ${
                                user.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                              }`}>
                                {user.isActive ? '已啟用' : '已停用'}
                              </span>
                            </td>
                            <td className="px-4 py-2">
                              <button
                                onClick={() => handleToggleUserActive(user.id)}
                                className={`px-2 py-1 rounded text-sm text-white ${
                                  user.isActive ? 'bg-red-500 hover:bg-red-600' : 'bg-green-500 hover:bg-green-600'
                                }`}
                              >
                                {user.isActive ? '停用' : '啟用'}
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* 統計資訊 */}
              {activeTab === 'stats' && stats && (
                <div>
                  <h2 className="text-3xl font-bold mb-6">統計資訊</h2>
                  <div className="grid grid-cols-2 gap-6 mb-6">
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-gray-600 text-sm font-bold">總行程數</h3>
                      <p className="text-3xl font-bold text-blue-600">{stats.totalTrips}</p>
                    </div>
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-gray-600 text-sm font-bold">總使用者</h3>
                      <p className="text-3xl font-bold text-green-600">{stats.totalUsers}</p>
                    </div>
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-gray-600 text-sm font-bold">總預訂數</h3>
                      <p className="text-3xl font-bold text-purple-600">{stats.totalBookings}</p>
                    </div>
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-gray-600 text-sm font-bold">總營收</h3>
                      <p className="text-3xl font-bold text-red-600">NT${stats.totalRevenue?.toLocaleString()}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-6">
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-lg font-bold mb-4">最新使用者</h3>
                      <ul className="space-y-2">
                        {stats.recentUsers?.map((user, idx) => (
                          <li key={idx} className="text-sm">
                            {user.email} - {new Date(user.dateJoined).toLocaleDateString('zh-TW')}
                          </li>
                        ))}
                      </ul>
                    </div>
                    <div className="bg-white p-6 rounded-lg shadow">
                      <h3 className="text-lg font-bold mb-4">各國預訂數</h3>
                      <ul className="space-y-2">
                        {stats.bookingsByCountry?.map((item, idx) => (
                          <li key={idx} className="text-sm">
                            {item.country} - {item.count} 筆
                          </li>
                        ))}
                      </ul>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
