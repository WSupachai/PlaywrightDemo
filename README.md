# 🎭 Playwright Demo (.NET)

โปรเจกต์สำหรับทำ Automated Testing บน .NET 9 ร่วมกับ Microsoft Playwright

---

## 🛠️ ขั้นตอนการติดตั้ง (Installation)

เลือกก๊อปปี้คำสั่งด้านล่างนี้ไปรันทีละขั้นตอนใน Terminal:

| ลำดับ | ขั้นตอนการทำงาน | คำสั่งที่ต้องรัน (คลิกปุ่ม Copy ขวามือ) |
| :---: | :--- | :--- |
| **1** | **ติดตั้ง Package** | `dotnet add package Microsoft.Playwright` |
| **2** | **Build โปรเจกต์** | `dotnet build` |
| **3** | **ติดตั้ง Browsers** | `powershell.exe -ExecutionPolicy Bypass -File bin\Debug\net9.0\playwright.ps1 install` |


