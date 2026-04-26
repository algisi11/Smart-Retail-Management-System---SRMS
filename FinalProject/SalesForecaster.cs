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

        // ✅ دالة مركزية لحساب windowSize الآمن رياضياً لـ SSA
        private int CalculateSafeWindowSize(int dataCount)
        {
            int maxSafeWindow = (dataCount / 2) - 1;
            return Math.Max(2, Math.Min(7, maxSafeWindow));
        }

        // =======================================================
        // 3. المحرك الجماعي: يعالج جميع المنتجات دفعة واحدة (معدل بشبكة الأمان)
        // =======================================================
        public List<ForecastReportItem> RunInventoryForecast(Action<string> onProgress = null)
        {
            void Report(string msg) => onProgress?.Invoke(msg);

            List<ForecastReportItem> finalReport = new List<ForecastReportItem>();

            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-30);

            Report("⏳ جاري جلب بيانات المبيعات من قاعدة البيانات...");
            DataTable rawData = GetRawSalesData(startDate, endDate);

            if (rawData.Rows.Count == 0)
            {
                Report("⚠️ لا توجد مبيعات مسجلة خلال الـ 30 يوماً الماضية.");
                return finalReport;
            }

            var groupedByProduct = rawData.AsEnumerable()
                .GroupBy(row => row.Field<string>("ProductName"))
                .ToList();

            int totalProducts = groupedByProduct.Count;
            int processedCount = 0;

            Report($"✅ تم جلب البيانات. جاري تحليل {totalProducts} منتج...");
            MLContext mlContext = new MLContext(seed: 42);

            foreach (var productGroup in groupedByProduct)
            {
                string productName = productGroup.Key;
                processedCount++;
                Report($"⏳ [{processedCount}/{totalProducts}] جاري تحليل: {productName}");

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

                // تخطي المنتجات التي بياناتها كلها أصفار 
                if (totalHistoricalSales == 0f)
                {
                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = 0,
                        Status = "❄️ لم يُباع خلال الشهر"
                    });
                    continue;
                }

                try
                {
                    IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);
                    int dataCount = continuousHistory.Count;
                    int safeWindowSize = CalculateSafeWindowSize(dataCount);

                    var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                        outputColumnName: nameof(ProductSalesPrediction.ForecastedSales),
                        inputColumnName: nameof(ProductSalesData.Quantity),
                        windowSize: safeWindowSize,
                        seriesLength: dataCount,
                        trainSize: dataCount,
                        horizon: 7
                    );

                    SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);
                    var forecastEngine = forecaster.CreateTimeSeriesEngine<ProductSalesData, ProductSalesPrediction>(mlContext);

                    ProductSalesPrediction prediction = forecastEngine.Predict();

                    float totalPredictedRaw = prediction.ForecastedSales.Sum(s => Math.Max(0, s));
                    int finalPredictedInt = (int)Math.Round(totalPredictedRaw);

                    // 🌟 الحل الهندسي: شبكة الأمان (Smart Fallback)
                    if (finalPredictedInt == 0 && totalHistoricalSales > 0)
                    {
                        float dailyAverage = totalHistoricalSales / dataCount;
                        finalPredictedInt = (int)Math.Ceiling(dailyAverage * 7);
                    }

                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = finalPredictedInt,
                        Status = finalPredictedInt >= 5 ? "🔥 يحتاج تعزيز" : "📊 مبيعات مستقرة"
                    });

                    forecastEngine.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SalesForecaster] فشل التنبؤ للمنتج '{productName}': {ex.Message}");
                    Report($"⚠️ تعذر تحليل: {productName}");

                    finalReport.Add(new ForecastReportItem
                    {
                        ProductName = productName,
                        TotalPredictedNext7Days = 0,
                        Status = "⚠️ خطأ في التنبؤ"
                    });
                }
            }

            Report($"✅ اكتمل التحليل! تم معالجة {totalProducts} منتج بنجاح.");
            return finalReport.OrderByDescending(r => r.TotalPredictedNext7Days).ToList();
        }

        // =======================================================
        // 4. محرك التنبؤ الفردي (يستخدم من شاشة AI Chat - معدل)
        // =======================================================
        public float[] PredictFutureSales(List<ProductSalesData> salesHistory, int daysToPredict = 7)
        {
            if (salesHistory == null || salesHistory.Count < 14)
                throw new Exception("البيانات غير كافية. نحتاج 14 يوماً على الأقل.");

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

            float totalHistoricalSales = continuousHistory.Sum(x => x.Quantity);
            if (totalHistoricalSales == 0f)
                throw new Exception("جميع بيانات المبيعات أصفار، لا يمكن إجراء التنبؤ.");

            MLContext mlContext = new MLContext(seed: 42);

            try
            {
                IDataView dataView = mlContext.Data.LoadFromEnumerable(continuousHistory);
                int dataCount = continuousHistory.Count;
                int safeWindowSize = CalculateSafeWindowSize(dataCount);

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(ProductSalesPrediction.ForecastedSales),
                    inputColumnName: nameof(ProductSalesData.Quantity),
                    windowSize: safeWindowSize,
                    seriesLength: dataCount,
                    trainSize: dataCount,
                    horizon: daysToPredict
                );

                SsaForecastingTransformer forecaster = forecastingPipeline.Fit(dataView);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<ProductSalesData, ProductSalesPrediction>(mlContext);

                ProductSalesPrediction prediction = forecastEngine.Predict();

                float[] finalResult = prediction.ForecastedSales
                    .Select(sales => (float)Math.Round(Math.Max(0, sales)))
                    .ToArray();

                // 🌟 الحل الهندسي (Smart Fallback للمصفوفة الفردية)
                if (finalResult.All(x => x == 0f))
                {
                    float dailyAverage = totalHistoricalSales / dataCount;
                    float smoothPrediction = (float)Math.Ceiling(dailyAverage);
                    for (int i = 0; i < finalResult.Length; i++)
                    {
                        finalResult[i] = smoothPrediction;
                    }
                }

                forecastEngine.Dispose();
                return finalResult;
            }
            catch (Exception ex)
            {
                throw new Exception("حدث خطأ أثناء التنبؤ الفردي: " + ex.Message);
            }
        }

        // =======================================================
        // 5. دالة جلب البيانات من SQL
        // =======================================================
        private DataTable GetRawSalesData(DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();

            string query = @"
                SELECT 
                    p.ProductName, 
                    CAST(i.InvoiceDate AS DATE) AS SaleDate, 
                    SUM(d.Quantity) AS TotalQty
                FROM SalesInvoiceDetails d
                JOIN SalesInvoices i ON d.InvoiceID = i.InvoiceID
                JOIN Products p ON d.ProductID = p.ProductID
                WHERE CAST(i.InvoiceDate AS DATE) BETWEEN @Start AND @End
                GROUP BY p.ProductName, CAST(i.InvoiceDate AS DATE)
                ORDER BY p.ProductName, SaleDate";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Start", startDate);
                cmd.Parameters.AddWithValue("@End", endDate);

                cmd.CommandTimeout = 60;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }
    }
}