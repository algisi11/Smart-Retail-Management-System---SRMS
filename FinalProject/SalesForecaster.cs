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
        public string Status { get; set; }
    }

    // =======================================================
    // 2. المحرك الرئيسي لمعالجة جميع المنتجات (Batch Engine)
    // =======================================================
    public class SalesForecaster
    {
        private string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        // ✅ الـ callback الذي سيُستدعى في كل خطوة لتحديث الـ Label في الـ UI
        // يمكن تركه null إذا استُدعيت الدالة بدون UI (مثلاً من frmAIChat)
        private Action<string> _progressCallback;

        // دالة مساعدة داخلية لإرسال التحديث بأمان
        private void Report(string message)
        {
            _progressCallback?.Invoke(message);
        }

        // =======================================================
        // RunInventoryForecast — مع دعم تحديث الـ Label خطوة بخطوة
        // =======================================================
        public List<ForecastReportItem> RunInventoryForecast(Action<string> progressCallback = null)
        {
            _progressCallback = progressCallback;

            List<ForecastReportItem> finalReport = new List<ForecastReportItem>();

            // --- الخطوة 1: جلب البيانات ---
            Report("📡 الخطوة 1 من 5: جاري الاتصال بقاعدة البيانات...");

            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-60);

            DataTable rawData = GetRawSalesData(startDate, endDate);

            if (rawData.Rows.Count == 0)
            {
                Report("⚠️ لا توجد بيانات مبيعات في آخر 60 يوماً.");
                return finalReport;
            }

            var groupedByProduct = rawData.AsEnumerable()
                .GroupBy(row => row.Field<string>("ProductName"))
                .ToList();

            int totalProducts = groupedByProduct.Count;

            // --- الخطوة 2: بناء التسلسل الزمني ---
            Report($"🗂️ الخطوة 2 من 5: تم جلب ({totalProducts}) منتج — جاري بناء التسلسل الزمني...");

            MLContext mlContext = new MLContext(seed: 0);
            int currentIndex = 0;

            foreach (var productGroup in groupedByProduct)
            {
                currentIndex++;
                string productName = productGroup.Key;

                // --- الخطوة 3: تدريب النموذج لكل منتج ---
                Report($"🧠 الخطوة 3 من 5: جاري تدريب نموذج SSA للمنتج ({currentIndex}/{totalProducts}): {productName}...");

                List<ProductSalesData> continuousHistory = new List<ProductSalesData>();
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                {
                    var dayRecord = productGroup.FirstOrDefault(r => r.Field<DateTime>("SaleDate") == d);
                    continuousHistory.Add(new ProductSalesData
                    {
                        Date = d,
                        Quantity = dayRecord != null ? Convert.ToSingle(dayRecord["TotalQty"]) : 0f
                    });
                }

                float totalHistoricalSales = continuousHistory.Sum(x => x.Quantity);

                if (totalHistoricalSales == 0f)
                {
                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = 0,
                        Status = "❄️ لا مبيعات في الـ 60 يوم الماضية"
                    });
                    continue;
                }

                int dataCount = continuousHistory.Count;
                int windowSize = Math.Max(3, Math.Min(7, dataCount / 4));

                try
                {
                    // --- الخطوة 4: التنبؤ ---
                    Report($"📊 الخطوة 4 من 5: تشغيل خوارزمية SSA (Singular Spectrum Analysis) للمنتج ({currentIndex}/{totalProducts}): {productName}...");

                    IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);

                    var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(ProductSalesPrediction.ForecastedSales),
                        inputColumnName: nameof(ProductSalesData.Quantity),
                        windowSize: windowSize,
                        seriesLength: dataCount,
                        trainSize: dataCount,
                        horizon: 7
                    );

                    SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);
                    var forecastEngine = forecaster.CreateTimeSeriesEngine<ProductSalesData, ProductSalesPrediction>(mlContext);

                    ProductSalesPrediction prediction = forecastEngine.Predict();
                    forecastEngine.Dispose();

                    float totalPredictedRaw = prediction.ForecastedSales.Sum(s => Math.Max(0f, s));
                    int finalPredictedInt = (int)Math.Round(totalPredictedRaw);

                    // Fallback بالمتوسط إذا أعطى النموذج صفراً
                    if (finalPredictedInt == 0)
                    {
                        float avgDailySales = totalHistoricalSales / dataCount;
                        finalPredictedInt = (int)Math.Ceiling(avgDailySales * 7);
                        Report($"⚡ [{productName}]: النموذج أعطى صفراً، تم التحويل للمتوسط الحسابي ({finalPredictedInt} وحدة).");
                    }

                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = finalPredictedInt,
                        Status = finalPredictedInt > 0 ? "🔥 يحتاج تعزيز" : "❄️ مبيعات ضعيفة"
                    });
                }
                catch
                {
                    float avgDailySales = totalHistoricalSales / dataCount;
                    int fallbackQty = (int)Math.Ceiling(avgDailySales * 7);

                    Report($"⚠️ فشل نموذج SSA للمنتج [{productName}]، تم استخدام المتوسط الحسابي ({fallbackQty} وحدة).");

                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = fallbackQty,
                        Status = fallbackQty > 0
                                                    ? "📊 تقدير بالمتوسط (بيانات غير كافية للنموذج)"
                                                    : "❄️ لا توجد بيانات كافية"
                    });
                }
            }

            // --- الخطوة 5: الانتهاء ---
            int needsStock = finalReport.Count(r => r.Status.Contains("يحتاج تعزيز"));
            Report($"✅ الخطوة 5 من 5: اكتمل التحليل! — تم تحليل ({totalProducts}) منتج، ({needsStock}) منتج يحتاج تعزيز فوري.");

            return finalReport.OrderByDescending(r => r.TotalPredictedNext7Days).ToList();
        }

        // =======================================================
        // 3. محرك التنبؤ الفردي (يستخدم من شاشة المحادثة الذكية)
        // =======================================================
        public float[] PredictFutureSales(List<ProductSalesData> salesHistory, int daysToPredict = 7)
        {
            if (salesHistory == null || salesHistory.Count == 0)
                throw new Exception("لا توجد بيانات مبيعات لهذا المنتج.");

            if (salesHistory.Count < 7)
                throw new Exception("البيانات غير كافية. نحتاج 7 أيام على الأقل.");

            DateTime minDate = salesHistory.Min(s => s.Date);
            DateTime maxDate = salesHistory.Max(s => s.Date);

            List<ProductSalesData> continuousHistory = new List<ProductSalesData>();
            for (DateTime d = minDate; d <= maxDate; d = d.AddDays(1))
            {
                var dayRecord = salesHistory.FirstOrDefault(s => s.Date.Date == d.Date);
                continuousHistory.Add(new ProductSalesData
                {
                    Date = d,
                    Quantity = dayRecord != null ? dayRecord.Quantity : 0f
                });
            }

            float totalSales = continuousHistory.Sum(x => x.Quantity);
            MLContext mlContext = new MLContext(seed: 0);

            try
            {
                IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);
                int dataCount = continuousHistory.Count;
                int windowSize = Math.Max(3, Math.Min(7, dataCount / 4));

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
                forecastEngine.Dispose();

                float[] result = prediction.ForecastedSales
                    .Select(s => (float)Math.Round(Math.Max(0f, s)))
                    .ToArray();

                if (result.All(v => v == 0f) && totalSales > 0)
                {
                    float avgDaily = totalSales / dataCount;
                    for (int i = 0; i < result.Length; i++)
                        result[i] = (float)Math.Ceiling(avgDaily);
                }

                return result;
            }
            catch (Exception ex)
            {
                if (totalSales > 0)
                {
                    float avgDaily = totalSales / continuousHistory.Count;
                    float[] fallback = new float[daysToPredict];
                    for (int i = 0; i < daysToPredict; i++)
                        fallback[i] = (float)Math.Ceiling(avgDaily);
                    return fallback;
                }

                throw new Exception("حدث خطأ أثناء التنبؤ الفردي: " + ex.Message);
            }
        }

        // =======================================================
        // 4. دالة مساعدة لجلب البيانات من SQL
        // =======================================================
        private DataTable GetRawSalesData(DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT 
                    p.ProductName, 
                    CAST(i.InvoiceDate AS DATE) AS SaleDate, 
                    SUM(d.Quantity)             AS TotalQty
                FROM SalesInvoiceDetails d
                JOIN SalesInvoices i ON d.InvoiceID  = i.InvoiceID
                JOIN Products      p ON d.ProductID  = p.ProductID
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