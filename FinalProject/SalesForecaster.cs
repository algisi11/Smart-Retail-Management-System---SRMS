using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace FinalProject
{
    // =======================================================
    // 1. هياكل البيانات (Data Models)
    // =======================================================
    public class ProductSalesData
    {
        public DateTime Date { get; set; }
        public float Quantity { get; set; }
    }

    public class ProductSalesPrediction
    {
        public float[] ForecastedSales { get; set; }
    }

    public class ForecastReportItem
    {
        public string ProductName { get; set; }
        public int TotalPredictedNext7Days { get; set; }
        public string Status { get; set; } // مثال: "طلب عاجل", "مخزون كافي"
    }

    // =======================================================
    // 2. المحرك الرئيسي لمعالجة جميع المنتجات (Batch Engine)
    // =======================================================
    public class SalesForecaster
    {
        private string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        public List<ForecastReportItem> RunInventoryForecast()
        {
            List<ForecastReportItem> finalReport = new List<ForecastReportItem>();

            // 1. تحديد النطاق الزمني (آخر 30 يوماً)
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-30);

            // 2. جلب البيانات الخام من قاعدة البيانات مجمعة حسب المنتج والتاريخ
            DataTable rawData = GetRawSalesData(startDate, endDate);

            // 3. تجميع البيانات في الذاكرة (C# LINQ) لمعالجتها منتجاً بمنتج
            var groupedByProduct = rawData.AsEnumerable()
                .GroupBy(row => row.Field<string>("ProductName"))
                .ToList();

            // تجهيز بيئة ML.NET مرة واحدة فقط خارج الـ Loop لتوفير الرام
            MLContext mlContext = new MLContext();

            foreach (var productGroup in groupedByProduct)
            {
                string productName = productGroup.Key;

                // 🌟 السحر هنا: بناء تسلسل زمني متصل لـ 30 يوماً، وملء الأيام الفارغة بصفر
                List<ProductSalesData> continuousHistory = new List<ProductSalesData>();
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    var dayRecord = productGroup.FirstOrDefault(r => r.Field<DateTime>("SaleDate") == d);
                    continuousHistory.Add(new ProductSalesData
                    {
                        Date = d,
                        Quantity = dayRecord != null ? Convert.ToSingle(dayRecord["TotalQty"]) : 0f // 0 إذا لم يباع شيء
                    });
                }

                // 4. إرسال البيانات المتصلة للذكاء الاصطناعي للتنبؤ
                try
                {
                    IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);

                    var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(ProductSalesPrediction.ForecastedSales),
                        inputColumnName: nameof(ProductSalesData.Quantity),
                        windowSize: 7,       // دورة أسبوعية
                        seriesLength: 30,    // طول البيانات (30 يوماً)
                        trainSize: 30,       // حجم التدريب
                        horizon: 7           // التنبؤ بـ 7 أيام قادمة
                    );

                    SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);
                    var forecastEngine = forecaster.CreateTimeSeriesEngine<ProductSalesData, ProductSalesPrediction>(mlContext);

                    ProductSalesPrediction prediction = forecastEngine.Predict();

                    // 1. نحسب المجموع الخام
                    float totalPredictedRaw = prediction.ForecastedSales.Sum(s => Math.Max(0, s));

                    // 2. نقوم بالتقريب إلى عدد صحيح أولاً
                    int finalPredictedInt = (int)Math.Round(totalPredictedRaw);

                    // 3. نعتمد على الرقم المقرب في إعطاء الحالة لكي لا يحدث تناقض
                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = finalPredictedInt,
                        Status = finalPredictedInt > 0 ? "🔥 يحتاج تعزيز" : "❄️ مبيعات ضعيفة"
                    });
                    // تنظيف محرك هذا المنتج فقط
                    forecastEngine.Dispose();
                }
                catch
                {
                    // إذا فشل التنبؤ لمنتج معين (مثلاً بياناته كلها أصفار)، نتجاوزه بصمت
                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = 0,
                        Status = "لا توجد بيانات كافية"
                    });
                }
            }

            return finalReport.OrderByDescending(r => r.TotalPredictedNext7Days).ToList();
        }
        // =======================================================
        // 4. محرك التنبؤ الفردي (يستخدم بواسطة شاشة المحادثة الذكية AI Chat)
        // =======================================================
        public float[] PredictFutureSales(List<ProductSalesData> salesHistory, int daysToPredict = 7)
        {
            if (salesHistory == null || salesHistory.Count < 14)
            {
                throw new Exception("البيانات غير كافية. نحتاج 14 يوماً على الأقل.");
            }

            // 🌟 معالجة فخ التواريخ المفقودة للمنتج الفردي
            DateTime minDate = salesHistory.Min(s => s.Date);
            DateTime maxDate = salesHistory.Max(s => s.Date);

            List<ProductSalesData> continuousHistory = new List<ProductSalesData>();
            for (DateTime d = minDate; d <= maxDate; d = d.AddDays(1))
            {
                var dayRecord = salesHistory.FirstOrDefault(s => s.Date.Date == d.Date);
                continuousHistory.Add(new ProductSalesData
                {
                    Date = d,
                    Quantity = dayRecord != null ? dayRecord.Quantity : 0f // إذا لم يُبع نضع 0
                });
            }

            MLContext mlContext = new MLContext();

            try
            {
                IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);

                int dataCount = continuousHistory.Count;
                int windowSize = dataCount >= 30 ? 7 : (dataCount / 2); // تحديد نافذة التدريب ديناميكياً

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(ProductSalesPrediction.ForecastedSales),
                    inputColumnName: nameof(ProductSalesData.Quantity),
                    windowSize: windowSize,
                    seriesLength: dataCount,
                    trainSize: dataCount,
                    horizon: daysToPredict
                );

                SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<ProductSalesData, ProductSalesPrediction>(mlContext);

                ProductSalesPrediction prediction = forecastEngine.Predict();

                // تنظيف الأرقام السلبية والكسور
                float[] finalResult = prediction.ForecastedSales.Select(sales => (float)Math.Round(Math.Max(0, sales))).ToArray();

                forecastEngine.Dispose();

                return finalResult;
            }
            catch (Exception ex)
            {
                throw new Exception("حدث خطأ أثناء التنبؤ الفردي: " + ex.Message);
            }
        }
        // =======================================================
        // 3. دالة مساعدة لجلب البيانات من الـ SQL
        // =======================================================
        private DataTable GetRawSalesData(DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT p.ProductName, CAST(i.InvoiceDate AS DATE) AS SaleDate, SUM(d.Quantity) AS TotalQty
                FROM SalesInvoiceDetails d
                JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                JOIN Products p ON d.ProductID = p.ProductID
                WHERE CAST(i.InvoiceDate AS DATE) BETWEEN @Start AND @End
                GROUP BY p.ProductName, CAST(i.InvoiceDate AS DATE)";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Start", startDate);
                cmd.Parameters.AddWithValue("@End", endDate);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }
    }
}