using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FinalProject
{
    // كلاس للعمل بـ Llama 3 عبر سيرفرات Groq الخارقة السرعة
    public class GroqLlamaClient
    {
        // ⚠️ تحذير: تجنب رفع هذا المفتاح على الإنترنت العام. ضع مفتاحك الحقيقي هنا:
        private readonly string apiKey = System.IO.File.ReadAllText("apikey.txt").Trim();
        private readonly string endpoint = "https://api.groq.com/openai/v1/chat/completions";

        // =========================================================
        // 🌟 ذاكرة الذكاء الاصطناعي (مبنية على معيار OpenAI)
        // =========================================================
        private List<object> conversationHistory = new List<object>();

        public async Task<string> AskAIAsync(string reportData, string userQuestion, string imagePath = null)
        {
            try
            {
                // إجبار البرنامج على استخدام بروتوكولات التشفير الحديثة
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

                // 1. تنظيف البيانات
                string cleanReportData = string.IsNullOrWhiteSpace(reportData) ? "لا توجد بيانات." : reportData.Trim();

                // 2. هندسة الأوامر المتقدمة (Executive Prompt Engineering)
                string imageWarning = !string.IsNullOrEmpty(imagePath)
                    ? "\n[تنبيه مخفي: المستخدم حاول إرفاق صورة للمنتج، ولكنك تعمل حالياً في وضع 'Llama السريع' الذي لا يدعم الرؤية. اعتذر له بلطف وبجملة واحدة عن عدم قدرتك على رؤية الصورة، ثم أجب على سؤاله بناءً على بيانات المبيعات فقط.]"
                    : "";

                string systemPrompt = $@"أنت 'المستشار الذكي' لنظام إدارة مستودعات ومبيعات متطور. 
قواعدك الأساسية والصارمة في الرد:
1. لا تقم أبداً بإعادة سرد البيانات الخام للمستخدم على شكل قوائم مملة.
2. استخدم أسلوباً احترافياً، أنيقاً، ومرتباً يشبه التقارير التنفيذية.
3. استخدم الرموز التعبيرية المناسبة لكل قسم (💰 للمبيعات، 📦 للمخزون، 🚨 للنواقص، 🚚 للموردين).
4. استخدم الأسطر الفارغة لفصل الأفكار وتسهيل القراءة للعين.
5. ركز على إعطاء 'رؤى' (Insights) مفيدة. مثلاً: الفت الانتباه للمنتجات التي توشك على النفاذ، أو المورد الذي لديه نسبة تلف عالية، وتجاهل المنتجات التي حالتها ممتازة إلا إذا سألك المستخدم عنها.
6. اختم ردك دائماً بنصيحة قصيرة أو اقتراح لخطوتك القادمة.

[بيانات النظام الحالية المتاحة لك للتحليل:
{cleanReportData}]{imageWarning}";

                // 💡 التصحيح المنطقي: تحديث بيانات النظام دائماً لكي يرى أحدث النواقص وتغيرات المستودع
                if (conversationHistory.Count == 0)
                {
                    conversationHistory.Add(new { role = "system", content = systemPrompt });
                }
                else
                {
                    conversationHistory[0] = new { role = "system", content = systemPrompt }; // استبدال القديم بالجديد
                }

                // إضافة سؤال المستخدم للذاكرة
                conversationHistory.Add(new { role = "user", content = userQuestion });

                // 3. تجهيز جسم الطلب بصيغة JSON المعتمدة لـ Groq
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant", // النسخة الأحدث والأسرع
                    messages = conversationHistory,
                    temperature = 0.7, // نسبة الإبداع
                    max_tokens = 1024
                };

                string jsonString = JsonConvert.SerializeObject(requestBody);

                // 🛡️ هندسة المحاولة التلقائية
                int maxRetries = 3;
                int delayMilliseconds = 1500;

                using (HttpClient client = new HttpClient())
                {
                    // 🚀 مصادقة Groq (Bearer Token)
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                    for (int i = 0; i < maxRetries; i++)
                    {
                        using (var content = new StringContent(jsonString, Encoding.UTF8, "application/json"))
                        {
                            // إرسال الطلب
                            HttpResponseMessage response = await client.PostAsync(endpoint, content);
                            string responseString = await response.Content.ReadAsStringAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                JObject parsedJson = JObject.Parse(responseString);
                                string aiReplyRaw = parsedJson["choices"][0]["message"]["content"].ToString();

                                // إضافة الرد للذاكرة للاحتفاظ بالسياق (اسم المتحدث هنا assistant وليس model)
                                conversationHistory.Add(new { role = "assistant", content = aiReplyRaw });

                                // 1. تنظيف الرموز الخاصة بـ Markdown
                                string cleanReply = aiReplyRaw.Replace("**", "").Replace("*", "-");

                                // 2. 🚀 السحر هنا: تحويل فواصل أسطر السيرفر (\n) إلى فواصل الويندوز (\r\n)
                                string windowsFormattedReply = cleanReply.Replace("\n", "\r\n");

                                return "\r\n" + windowsFormattedReply.Trim() + "\r\n";
                            }
                            else if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                            {
                                // 429: Rate Limit (تجاوزت الحد المسموح في الدقيقة)
                                // 503: Server Busy
                                if (i == maxRetries - 1)
                                {
                                    conversationHistory.RemoveAt(conversationHistory.Count - 1);
                                    return "عذراً، سيرفرات Llama مشغولة أو تجاوزت الحد المسموح. يرجى المحاولة لاحقاً.";
                                }
                                await Task.Delay(delayMilliseconds);
                                delayMilliseconds *= 2;
                            }
                            else
                            {
                                // خطأ قاتل
                                conversationHistory.RemoveAt(conversationHistory.Count - 1);
                                return $"فشل الاتصال بـ Groq.\nكود الخطأ: {(int)response.StatusCode}\nالتفاصيل: {responseString}";
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