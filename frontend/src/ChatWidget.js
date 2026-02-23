import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

// 定義後端 Base URL
const API_BASE = "http://localhost:5000";

export default function ChatWidget() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([
    { from: "bot", text: "請輸入想去的地方與天數，例如：東京三日遊、韓國六日親子行程。" },
  ]);
  const [input, setInput] = useState("");

  const toggleOpen = () => setOpen(!open);

  const sendMessage = async () => {
    if (!input.trim()) return;

    const userMsg = { from: "user", text: input };
    setMessages((msgs) => [...msgs, userMsg]);
    setInput("");

    try {
      const res = await fetch(`${API_BASE}/api/Chat`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message: input, topK: 3 }),
      });

      if (!res.ok) throw new Error("API 連線失敗");
      const data = await res.json();

      let newBotMsgs = [];

      // 1. 顯示 AI 的文字回覆 (對應 .NET Response 欄位)
      // 注意：.NET API 可能回傳 aiResponse 或 response，請確認欄位名
      const botText = data.aiResponse || data.response;
      if (botText) {
        newBotMsgs.push({ from: "bot", text: botText });
      }

      // 2. 處理行程推薦列表 (加入圖片顯示並移除 Emoji)
      const trips = data.recommendedTrips || data.recommended_trips;

      if (trips && trips.length > 0) {
        const tripCards = trips.map((trip, i) => ({
          from: "bot",
          text: (
            <div key={i} style={{ borderTop: "1px solid #ddd", marginTop: 8, paddingTop: 8, display: "flex", gap: "10px", alignItems: "center" }}>
              <img 
                src={trip.image ? `${API_BASE}${trip.image}` : ""} 
                alt="" 
                style={{ width: "60px", height: "60px", borderRadius: "4px", objectCover: "cover", backgroundColor: "#f0f0f0" }} 
              />
              <div style={{ flex: 1 }}>
                <div style={{ fontWeight: "bold", color: "#333", fontSize: "0.95em" }}>{trip.name}</div>
                <div style={{ fontSize: "0.8em", color: "#888", marginBottom: "4px" }}>
                  {trip.city}, {trip.country}
                </div>
                <button 
                  onClick={() => { setOpen(false); navigate(`/trips/${trip.id}`); }}
                  style={{ color: "#000", fontSize: "0.8em", fontWeight: "bold", padding: "2px 0", border: "none", background: "none", borderBottom: "1px solid #000", cursor: "pointer" }}
                >
                  查看行程
                </button>
              </div>
            </div>
          ),
        }));
        newBotMsgs = [...newBotMsgs, ...tripCards];
      }

      if (newBotMsgs.length === 0) {
        newBotMsgs.push({ from: "bot", text: "目前沒有找到符合的行程，請嘗試輸入其他目的地。" });
      }

      setMessages((msgs) => [...msgs, ...newBotMsgs]);

    } catch (err) {
      console.error("Chat Error:", err);
      setMessages((msgs) => [...msgs, { from: "bot", text: "無法連線至伺服器，請稍後再試。" }]);
    }
  };

  return (
    <>
      {open && (
        <div style={{ position: "fixed", bottom: 90, right: 20, width: 340, height: 480, boxShadow: "0 8px 24px rgba(0,0,0,0.15)", borderRadius: "12px", backgroundColor: "white", fontFamily: "-apple-system, BlinkMacSystemFont, sans-serif", display: "flex", flexDirection: "column", zIndex: 9999, overflow: "hidden", border: "1px solid #eee" }}>
          {/* Header */}
          <div style={{ padding: "15px", backgroundColor: "#000", color: "#fff", fontWeight: "bold", fontSize: "14px", letterSpacing: "1px" }}>
            TRAVEL ASSISTANT
          </div>
          
          {/* Messages */}
          <div style={{ flex: 1, overflowY: "auto", padding: "15px", backgroundColor: "#fff" }}>
            {messages.map((m, i) => (
              <div key={i} style={{ textAlign: m.from === "user" ? "right" : "left", marginBottom: 12 }}>
                <div style={{ 
                  display: "inline-block", 
                  padding: "10px 14px", 
                  borderRadius: m.from === "user" ? "15px 15px 2px 15px" : "15px 15px 15px 2px", 
                  backgroundColor: m.from === "user" ? "#000" : "#f4f4f4", 
                  color: m.from === "user" ? "#fff" : "#333", 
                  maxWidth: "85%", 
                  fontSize: "13px",
                  lineHeight: "1.5"
                }}>
                  {m.text}
                </div>
              </div>
            ))}
          </div>

          {/* Input */}
          <div style={{ padding: "15px", borderTop: "1px solid #f0f0f0", display: "flex", gap: "8px" }}>
            <input 
              type="text" 
              value={input} 
              onChange={(e) => setInput(e.target.value)} 
              onKeyDown={(e) => e.key === "Enter" && sendMessage()} 
              style={{ flex: 1, padding: "10px", borderRadius: "4px", border: "1px solid #ddd", fontSize: "13px", outline: "none" }} 
              placeholder="想去哪裡玩？" 
            />
            <button 
              onClick={sendMessage} 
              style={{ cursor: "pointer", backgroundColor: "#000", color: "#white", border: "none", borderRadius: "4px", padding: "0 15px", fontSize: "13px", fontWeight: "bold" }}
            >
              送出
            </button>
          </div>
        </div>
      )}
      
      {/* 懸浮按鈕：移除 Emoji 💬，改用文字或簡潔風格 */}
      <button 
        onClick={toggleOpen} 
        style={{ position: "fixed", bottom: 20, right: 20, width: "auto", height: "auto", padding: "12px 24px", borderRadius: "30px", backgroundColor: "#000", border: "none", color: "#fff", fontSize: "14px", fontWeight: "bold", cursor: "pointer", zIndex: 10000, boxShadow: "0 4px 12px rgba(0,0,0,0.2)", letterSpacing: "1px" }}
      >
        AI ASSISTANT
      </button>
    </>
  );
}