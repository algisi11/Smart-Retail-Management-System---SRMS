using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace FinalProject
{
    // 1. القالب الذي يقرأ فواتير الزبائن السابقة
    public class ProductEntry
    {
        public uint ProductA { get; set; }
        public uint ProductB { get; set; }
        public float CoPurchaseCount { get; set; }
    }

    // 2. القالب الذي يحمل نتيجة التوقع
    public class CopurchasePrediction
    {
        public float Score { get; set; }
    }

    // 3. المحرك الذكي للتوصيات
    public class SmartRecommender : IDisposable
    {
        private MLContext mlContext;
        private ITransformer trainedModel;
        private PredictionEngine<ProductEntry, CopurchasePrediction> predictionEngine;

        // HashSet يسرع البحث ويمنع التكرار تلقائياً
        private HashSet<uint> allAvailableProducts;
        private string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        // ✅ تتبع وقت آخر تدريب للسماح بإعادة التدريب الدورية
        private DateTime _lastTrainingTime = DateTime.MinValue;

        // ✅ خاصية للتحقق من جاهزية النموذج قبل الاستخدام
        public bool IsModelReady => predictionEngine != null && allAvailableProducts.Count > 0;

        // ✅ خاصية للتحقق مما إذا كان النموذج يحتاج إعادة تدريب (كل 30 دقيقة)
        public bool NeedsRetraining =>
            (DateTime.Now - _lastTrainingTime).TotalMinutes > 30 || !IsModelReady;

        public SmartRecommender()
        {
            mlContext = new MLContext(seed: 0);
            allAvailableProducts = new HashSet<uint>();
        }

        // -------------------------------------------------------------------
        // ✅ TrainModel المتزامنة (تُستدعى من Task.Run في الـ UI)
        // -------------------------------------------------------------------
        public void TrainModel()
        {
            List<ProductEntry> data = new List<ProductEntry>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT 
                        d1.ProductID AS ProductA, 
                        d2.ProductID AS ProductB, 
                        CAST(COUNT(*) AS REAL) AS CoPurchaseCount
                    FROM SalesInvoiceDetails d1
                    JOIN SalesInvoiceDetails d2 
                        ON d1.InvoiceID = d2.InvoiceID 
                        AND d1.ProductID != d2.ProductID
                    GROUP BY d1.ProductID, d2.ProductID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uint pA = Convert.ToUInt32(reader["ProductA"]);
                            uint pB = Convert.ToUInt32(reader["ProductB"]);

                            data.Add(new ProductEntry
                            {
                                ProductA = pA,
                                ProductB = pB,
                                CoPurchaseCount = Convert.ToSingle(reader["CoPurchaseCount"])
                            });

                            allAvailableProducts.Add(pB);
                        }
                    }
                }
            }

            // لا توجد فواتير سابقة للتعلم منها
            if (data.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[SmartRecommender] لا توجد بيانات كافية للتدريب.");
                return;
            }

            var dataView = mlContext.Data.LoadFromEnumerable(data);

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "ProductAEncoded",
                MatrixRowIndexColumnName = "ProductBEncoded",
                LabelColumnName = "CoPurchaseCount",
                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            var pipeline = mlContext.Transforms.Conversion
                .MapValueToKey("ProductAEncoded", "ProductA")
                .Append(mlContext.Transforms.Conversion.MapValueToKey("ProductBEncoded", "ProductB"))
                .Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

            // ✅ التخلص من المحرك القديم قبل إنشاء جديد لتحرير الذاكرة
            predictionEngine?.Dispose();

            trainedModel = pipeline.Fit(dataView);
            predictionEngine = mlContext.Model.CreatePredictionEngine<ProductEntry, CopurchasePrediction>(trainedModel);

            // ✅ تسجيل وقت التدريب
            _lastTrainingTime = DateTime.Now;

            System.Diagnostics.Debug.WriteLine(
                $"[SmartRecommender] اكتمل التدريب. عدد أزواج المنتجات: {data.Count}، المنتجات المتاحة: {allAvailableProducts.Count}");
        }

        // -------------------------------------------------------------------
        // ✅ نسخة async من TrainModel لاستدعائها مباشرة من UI بدون await Task.Run
        // -------------------------------------------------------------------
        public async Task TrainModelAsync()
        {
            await Task.Run(() => TrainModel());
        }

        // -------------------------------------------------------------------
        // الاقتراح بناءً على منتج واحد فقط
        // -------------------------------------------------------------------
        public int GetBestRecommendation(int currentProductId)
        {
            if (!IsModelReady) return -1;

            uint currentPId = (uint)currentProductId;
            float bestScore = float.MinValue;
            uint recommendedProduct = 0;

            try
            {
                foreach (var pId in allAvailableProducts)
                {
                    if (pId == currentPId) continue;

                    var prediction = predictionEngine.Predict(new ProductEntry
                    {
                        ProductA = currentPId,
                        ProductB = pId
                    });

                    if (prediction.Score > bestScore)
                    {
                        bestScore = prediction.Score;
                        recommendedProduct = pId;
                    }
                }
            }
            catch (Exception ex)
            {
                // ✅ النموذج قد ينهار مع منتجات لم يرها أثناء التدريب
                System.Diagnostics.Debug.WriteLine(
                    $"[SmartRecommender] خطأ في GetBestRecommendation: {ex.Message}");
                return -1;
            }

            return (bestScore > 0.01f && recommendedProduct != 0)
                ? (int)recommendedProduct
                : -1;
        }

        // -------------------------------------------------------------------
        // 🌟 الاقتراح بناءً على السلة بالكامل (الدالة الاحترافية)
        // -------------------------------------------------------------------
        public List<int> GetCartRecommendations(List<int> cartProductIds, int numberOfRecommendations = 2)
        {
            if (!IsModelReady || cartProductIds == null || cartProductIds.Count == 0)
                return new List<int>();

            Dictionary<uint, float> candidateScores = new Dictionary<uint, float>();

            try
            {
                foreach (var candidateId in allAvailableProducts)
                {
                    // لا تقترح منتجاً موجوداً بالفعل في السلة
                    if (cartProductIds.Contains((int)candidateId)) continue;

                    float totalScore = 0f;

                    // اختبار قوة ارتباط المنتج المرشح مع كل منتج في السلة
                    foreach (var cartItemId in cartProductIds)
                    {
                        var prediction = predictionEngine.Predict(new ProductEntry
                        {
                            ProductA = (uint)cartItemId,
                            ProductB = candidateId
                        });

                        totalScore += prediction.Score;
                    }

                    if (totalScore > 0.01f)
                        candidateScores[candidateId] = totalScore;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SmartRecommender] خطأ في GetCartRecommendations: {ex.Message}");
                return new List<int>();
            }

            // ترتيب من الأقوى للأضعف وأخذ أفضل النتائج
            return candidateScores
                .OrderByDescending(x => x.Value)
                .Take(numberOfRecommendations)
                .Select(x => (int)x.Key)
                .ToList();
        }

        // ✅ تطبيق IDisposable للتنظيف الصحيح عند إغلاق البرنامج
        public void Dispose()
        {
            predictionEngine?.Dispose();
        }
    }
}