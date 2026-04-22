# نظام المستودعات الذكي والتنبؤ بالمبيعات | Smart Inventory & AI Forecasting System 🚀

![License](https://img.shields.io/badge/License-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-Framework%204.7.2-purple.svg)
![ML.NET](https://img.shields.io/badge/AI-ML.NET-orange.svg)

نظام متكامل لإدارة التجزئة والمستودعات يعتمد على الذكاء الاصطناعي لتحليل البيانات والتنبؤ بالاحتياجات المستقبلية، مصمم لتوفير تجربة مستخدم عصرية وأداء محاسبي دقيق.

An integrated retail and warehouse management system that leverages AI to analyze data and predict future needs, designed to provide a modern user experience and accurate accounting performance.

## ✨ المميزات الرئيسية | Key Features

### 🛒 إدارة المبيعات والمشتريات (Sales & Purchasing)
- **نظام الكاشير الذكي:** إصدار فواتير مبيعات سريعة مع خصم تلقائي من المخزون.
- **إدارة التوريد:** تسجيل عمليات التوريد من الموردين وتتبع فواتير الشراء.
- **المرتجعات:** نظام معالجة المرتجعات (Refunds) مع تحديث فوري للمخزون وسجل التدقيق.

### 🤖 الذكاء الاصطناعي والتنبؤ (AI & Forecasting)
- **محرك SSA/SARIMA:** استخدام مكتبة `ML.NET` للتنبؤ بحجم المبيعات لـ 7 أيام قادمة لكل منتج.
- **تحليل النمط الزمني:** معالجة التواريخ المفقودة (Data Imputation) لضمان دقة التنبؤ.
- **التحليل الجماعي (Batch Processing):** واجهة منبثقة لتحليل كافة منتجات المستودع بضغطة زر.

### 📊 لوحة التحكم والتقارير (Dashboard & Analytics)
- **مخططات بيانية تفاعلية:** عرض المنتجات الأكثر مبيعاً والأعلى تدويراً.
- **حساب الأرباح:** تتبع إجمالي الربح وصافي الربح بعد خصم المصروفات.
- **تنبيهات النواقص:** نظام ذكي لتنبيه الأدمن عند وصول المنتجات لحد الطلب (Re-order Point).

### 🔒 الأمن والإدارة (Security & Admin)
- **سجل المراقبة (Audit Log):** تتبع كافة تحركات الموظفين (إضافة، تعديل، حذف) بالوقت والتاريخ.
- **النسخ الاحتياطي (Backup):** أداة متكاملة لأخذ نسخ احتياطية من قاعدة البيانات SQL Server وحفظها بأمان.
- **إدارة المصروفات:** نظام منبثق لتسجيل المصاريف النثرية (كهرباء، إيجار، رواتب).

## 🛠 التكنولوجيات المستخدمة | Tech Stack

* **Language:** C# / .NET Framework
* **Database:** Microsoft SQL Server
* **AI/ML:** ML.NET (Singular Spectrum Analysis - SSA)
* **UI/UX:** MaterialSkin 2.0 (Modern UI Design)
* **Libraries:** D3.js (for analytics - if applicable), BarcodeLib.

## 🚀 تنصيب المشروع | Installation

1.  **قاعدة البيانات (Database):**
    * قم بإنشاء قاعدة بيانات باسم `SmartInventoryDB` في SQL Server.
    * قم بتشغيل ملف السكربت `DatabaseScript.sql` (المرفق في المجلد) لإنشاء الجداول.

2.  **إعداد الاتصال (Connection String):**
    * تأكد من تعديل `connectionString` في ملفات المشروع ليطابق اسم السيرفر لديك.
    ```csharp
    string connectionString = @"Server=YOUR_SERVER_NAME;Database=SmartInventoryDB;Integrated Security=True;";
    ```

3.  **المتطلبات (Prerequisites):**
    * Visual Studio 2019 or later.
    * .NET Framework 4.7.2+.
    * NuGet Packages: `MaterialSkin.2`, `Microsoft.ML`, `Microsoft.ML.TimeSeries`.

## 📸 لقطات من النظام | Screenshots

| لوحة التحكم (Dashboard) | شاشة الكاشير (POS) | التنبؤ الذكي (AI Analysis) |
|---|---|---|
| ![Dashboard Placeholder] | ![POS Placeholder] | ![AI Placeholder] |

## 👨‍💻 تطوير | Developed By

* **الاسم:** مصطفى صالح

---
*تم تطوير هذا المشروع كجزء من مشروع التخرج / تطوير شخصي لنظم الإدارة الذكية.*
