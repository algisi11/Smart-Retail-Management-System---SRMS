using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq; // ضروري جداً لدوال الترتيب
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace FinalProject
{
    // 1. القالب الذي سيقرأ فواتير الزبائن السابقة
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

    // 3. المحرك الذكي (النسخة النهائية الشاملة)
    public class SmartRecommender
    {
        private MLContext mlContext;
        private ITransformer trainedModel;
        private PredictionEngine<ProductEntry, CopurchasePrediction> predictionEngine;

        // استخدام HashSet يسرع النظام ويمنع التكرار تلقائياً
        private HashSet<uint> allAvailableProducts;
        private string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        public SmartRecommender()
        {
            mlContext = new MLContext(seed: 0);
            allAvailableProducts = new HashSet<uint>();
        }

        // -------------------------------------------------------------------
        // دالة التدريب: تقرأ قاعدة البيانات وتبني "عقل" الذكاء الاصطناعي
        // -------------------------------------------------------------------
        public void TrainModel()
        {
            List<ProductEntry> data = new List<ProductEntry>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                // استعلام يربط المنتجات التي بيعت في نفس الفاتورة معاً
                string query = @"SELECT d1.ProductID AS ProductA, d2.ProductID AS ProductB, CAST(COUNT(*) AS REAL) AS CoPurchaseCount
                                 FROM SalesInvoiceDetails d1
                                 JOIN SalesInvoiceDetails d2 ON d1.InvoiceID = d2.InvoiceID AND d1.ProductID != d2.ProductID
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

                            data.Add(new ProductEntry { ProductA = pA, ProductB = pB, CoPurchaseCount = Convert.ToSingle(reader["CoPurchaseCount"]) });

                            // تخزين كل المنتجات المتاحة لاستخدامها لاحقاً في التوقعات
                            allAvailableProducts.Add(pB);
                        }
                    }
                }
            }

            if (data.Count == 0) return; // لا يوجد فواتير سابقة للتعلم منها

            var dataView = mlContext.Data.LoadFromEnumerable(data);

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "ProductAEncoded",
                MatrixRowIndexColumnName = "ProductBEncoded",
                LabelColumnName = "CoPurchaseCount",
                NumberOfIterations = 20,
                ApproximationRank = 100
            };

            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("ProductAEncoded", "ProductA")
                .Append(mlContext.Transforms.Conversion.MapValueToKey("ProductBEncoded", "ProductB"))
                .Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

            trainedModel = pipeline.Fit(dataView);
            predictionEngine = mlContext.Model.CreatePredictionEngine<ProductEntry, CopurchasePrediction>(trainedModel);
        }

        // -------------------------------------------------------------------
        // الدالة القديمة: تقترح بناءً على "منتج واحد" فقط
        // -------------------------------------------------------------------
        public int GetBestRecommendation(int currentProductId)
        {
            if (predictionEngine == null || allAvailableProducts.Count == 0) return -1;

            uint currentPId = (uint)currentProductId;
            float bestScore = float.MinValue;
            uint recommendedProduct = 0;

            foreach (var pId in allAvailableProducts)
            {
                if (pId == currentPId) continue; // منع اقتراح نفس المنتج

                var prediction = predictionEngine.Predict(new ProductEntry { ProductA = currentPId, ProductB = pId });

                if (prediction.Score > bestScore)
                {
                    bestScore = prediction.Score;
                    recommendedProduct = pId;
                }
            }

            return (bestScore > 0.01f && recommendedProduct != 0) ? (int)recommendedProduct : -1;
        }

        // -------------------------------------------------------------------
        // 🌟 الدالة الجديدة والاحترافية: تقترح بناءً على "السلة بالكامل"
        // -------------------------------------------------------------------
        public List<int> GetCartRecommendations(List<int> cartProductIds, int numberOfRecommendations = 2)
        {
            if (predictionEngine == null || allAvailableProducts.Count == 0 || cartProductIds.Count == 0)
                return new List<int>();

            Dictionary<uint, float> candidateScores = new Dictionary<uint, float>();

            // نمر على كل المنتجات المتوفرة في المستودع
            foreach (var candidateId in allAvailableProducts)
            {
                // لا تقترح منتجاً موجوداً بالفعل داخل السلة!
                if (cartProductIds.Contains((int)candidateId)) continue;

                float totalScore = 0f;

                // نختبر قوة ارتباط هذا المنتج المرشح مع *كل* منتج موجود في السلة حالياً
                foreach (var cartItemId in cartProductIds)
                {
                    var prediction = predictionEngine.Predict(new ProductEntry
                    {
                        ProductA = (uint)cartItemId,
                        ProductB = candidateId
                    });

                    totalScore += prediction.Score;
                }

                // إذا كان هناك ارتباط إيجابي نضفه لقائمة المنافسة
                if (totalScore > 0.01f)
                {
                    candidateScores.Add(candidateId, totalScore);
                }
            }

            // نرتب المنتجات من الأقوى ترابطاً للأضعف ونأخذ أفضل اختيارات
            var bestRecommendations = candidateScores
                .OrderByDescending(x => x.Value)
                .Take(numberOfRecommendations)
                .Select(x => (int)x.Key)
                .ToList();

            return bestRecommendations;
        }
    }
}