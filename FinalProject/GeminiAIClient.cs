using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FinalProject
{
    public class GeminiAIClient
    {
        // ⚠️ المفتاح السري الخاص بك
        private readonly string apiKey = "AIzaSyAyhKF8MZTAUSad9QcY8gk_DjWXn6EQK6o";

        // =========================================================
        // 🌟 ذاكرة الذكاء الاصطناعي
        // =========================================================
        private List<object> conversationHistory = new List<object>();

        public async Task<string> AskAIAsync(string reportData, string userQuestion, string imagePath = null)
        {
            try
            {
                // إجبار البرنامج على استخدام بروتوكولات التشفير الحديثة
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

                using (HttpClient client = new HttpClient())
                {
                    // 🌟 الموديل السريع والمستقر
                    string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={apiKey}";

                    string cleanReportData = string.IsNullOrWhiteSpace(reportData) ? "لا توجد بيانات." : reportData.Trim();

                    string currentMessage = $"[بيانات:\n{cleanReportData}]\n\nأجب باختصار.\nسؤال: {userQuestion}";

                    var partsList = new List<object>();
                    partsList.Add(new { text = currentMessage });

                    if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                    {
                        byte[] imageArray = File.ReadAllBytes(imagePath);
                        string base64ImageRepresentation = Convert.ToBase64String(imageArray);
                        string extension = Path.GetExtension(imagePath).Replace(".", "").ToLower();
                        string mimeType = extension == "png" ? "image/png" : (extension == "webp" ? "image/webp" : "image/jpeg");

                        partsList.Add(new
                        {
                            inline_data = new { mime_type = mimeType, data = base64ImageRepresentation }
                        });
                    }

                    // إضافة السؤال للذاكرة
                    conversationHistory.Add(new { role = "user", parts = partsList });

                    // تجهيز النص بصيغة JSON (خارج اللوب)
                    var requestBody = new { contents = conversationHistory };
                    string jsonString = JsonConvert.SerializeObject(requestBody);

                    // 🛡️ هندسة المحاولة التلقائية
                    int maxRetries = 3;
                    int delayMilliseconds = 2000;

                    for (int i = 0; i < maxRetries; i++)
                    {
                        // 🚀 الحل هنا: إنشاء الـ content داخل اللوب ليكون جديداً في كل محاولة
                        using (var content = new StringContent(jsonString, Encoding.UTF8, "application/json"))
                        {
                            // إرسال الطلب
                            HttpResponseMessage response = await client.PostAsync(requestUrl, content);
                            string responseString = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                JObject parsedJson = JObject.Parse(responseString);
                                string aiReplyRaw = parsedJson["candidates"][0]["content"]["parts"][0]["text"].ToString();

                                conversationHistory.Add(new { role = "model", parts = new[] { new { text = aiReplyRaw } } });

                                return "\n" + aiReplyRaw.Replace("**", "").Replace("*", "-").Trim() + "\n";
                            }
                            else if ((int)response.StatusCode == 503 || (int)response.StatusCode == 429)
                            {
                                // السيرفر مشغول
                                if (i == maxRetries - 1)
                                {
                                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                                    return "عذراً، سيرفرات الذكاء الاصطناعي عليها ضغط عالي جداً حالياً. يرجى المحاولة بعد قليل.";
                                }

                                // الانتظار ثم المحاولة
                                await Task.Delay(delayMilliseconds);
                                delayMilliseconds *= 2;
                            }
                            else
                            {
                                // خطأ قاتل آخر
                                conversationHistory.RemoveAt(conversationHistory.Count - 1);
                                return $"فشل الاتصال.\nكود الخطأ: {(int)response.StatusCode}\nالتفاصيل: {responseString}";
                            }
                        }
                    }

                    return "فشل الاتصال بعد عدة محاولات.";
                }
            }
            catch (Exception ex)
            {
                return "حدث خطأ غير متوقع في الكود: " + ex.Message;
            }
        }

        public void ClearMemory()
        {
            conversationHistory.Clear();
        }
    }
}