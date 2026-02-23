import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

// 取得 root 元素
const rootElement = document.getElementById('root');

// 增加判斷，確保抓得到 div 再渲染
if (rootElement) {
  const root = ReactDOM.createRoot(rootElement);
  root.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
} else {
  // 如果還是報錯，會在 Console 印出這行
  console.error("Critical Error: Could not find element with id 'root'.");
}
