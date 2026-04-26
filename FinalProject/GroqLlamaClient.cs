using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FinalProject
{
    // كلاس للعمل بـ Llama 3 عبر سيرفرات Groq الخارقة السرعة
    public class GroqLlamaClient : IDisposable
    {
        // ✅ المفتاح يُقرأ من ملف خارجي بدلاً من تضمينه في الكود
        private readonly string apiKey = System.IO.File.ReadAllText("apikey.txt").Trim();
        private readonly string endpoint = "https://api.groq.com/openai/v1/chat/completions";

        // =========================================================
        // ✅ HttpClient ثابت مشترك لتجنب socket exhaustion
        // =========================================================
        private static readonly HttpClient _sharedClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // =========================================================
        // 🌟 ذاكرة الذكاء الاصطناعي (مبنية على معيار OpenAI)
        // =========================================================
        private List<object> conversationHistory = new List<object>();

        // ✅ الحد الأقصى لعدد الرسائل في الذاكرة قبل التنظيف التلقائي
        private const int MAX_HISTORY_MESSAGES = 20;

        // ✅ خاصية لمعرفة حجم المحادثة الحالية من الخارج
        public int ConversationLength => conversationHistory.Count;

        // =========================================================
        // ✅ دالة AskAIAsync المحسّنة مع CancellationToken
        // =========================================================
        public async Task<string> AskAIAsync(
            string reportData,
            string userQuestion,
            string imagePath = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // إجبار البرنامج على استخدام بروتوكولات التشفير الحديثة
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12 |
                    System.Net.SecurityProtocolType.Tls13;

                // ✅ تنظيف الذاكرة تلقائياً إذا تجاوزت الحد الأقصى
                // نحتفظ برسالة النظام (index 0) + آخر 10 رسائل فقط
                if (conversationHistory.Count > MAX_HISTORY_MESSAGES)
                {
                    var systemMessage = conversationHistory.FirstOrDefault();
                    var recentMessages = conversationHistory
                        .Skip(conversationHistory.Count - 10)
                        .ToList();

                    conversationHistory.Clear();

                    if (systemMessage != null)
                        conversationHistory.Add(systemMessage);

                    conversationHistory.AddRange(recentMessages);
                }

                // 1. تنظيف البيانات
                string cleanReportData = string.IsNullOrWhiteSpace(reportData)
                    ? "لا توجد بيانات."
                    : reportData.Trim();

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

                // ✅ تحديث بيانات النظام دائماً لكي يرى أحدث التغييرات
                if (conversationHistory.Count == 0)
                    conversationHistory.Add(new { role = "system", content = systemPrompt });
                else
                    conversationHistory[0] = new { role = "system", content = systemPrompt };

                // إضافة سؤال المستخدم للذاكرة
                conversationHistory.Add(new { role = "user", content = userQuestion });

                // 3. تجهيز جسم الطلب
                var requestBody = new
                {
                    model = "llama-3.1-8b-instant",
                    messages = conversationHistory,
                    temperature = 0.7,
                    max_tokens = 1024
                };

                string jsonString = JsonConvert.SerializeObject(requestBody);

                // ✅ ضبط Authorization مرة واحدة فقط إذا لم تكن مضبوطة مسبقاً
                if (_sharedClient.DefaultRequestHeaders.Authorization == null)
                {
                    _sharedClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", apiKey);
                }

                // 🛡️ هندسة المحاولة التلقائية
                int maxRetries = 3;
                int delayMilliseconds = 1500;

                for (int i = 0; i < maxRetries; i++)
                {
                    // ✅ التحقق من إلغاء الطلب قبل كل محاولة
                    cancellationToken.ThrowIfCancellationRequested();

                    using (var content = new StringContent(jsonString, Encoding.UTF8, "application/json"))
                    {
                        HttpResponseMessage response = await _sharedClient
                            .PostAsync(endpoint, content, cancellationToken);

                        string responseString = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            JObject parsedJson = JObject.Parse(responseString);
                            string aiReplyRaw = parsedJson["choices"][0]["message"]["content"].ToString();

                            // إضافة الرد للذاكرة
                            conversationHistory.Add(new { role = "assistant", content = aiReplyRaw });

                            // تنظيف رموز Markdown وتحويل أسطر Linux لـ Windows
                            string cleanReply = aiReplyRaw
                                .Replace("**", "")
                                .Replace("*", "-");

                            string windowsFormattedReply = cleanReply.Replace("\n", "\r\n");

                            return "\r\n" + windowsFormattedReply.Trim() + "\r\n";
                        }
                        else if ((int)response.StatusCode == 429 || (int)response.StatusCode == 503)
                        {
                            // 429: Rate Limit | 503: Server Busy
                            if (i == maxRetries - 1)
                            {
                                // إزالة سؤال المستخدم من الذاكرة لأن الطلب فشل
                                if (conversationHistory.Count > 1)
                                    conversationHistory.RemoveAt(conversationHistory.Count - 1);

                                return "عذراً، سيرفرات Llama مشغولة أو تجاوزت الحد المسموح. يرجى المحاولة لاحقاً.";
                            }

                            // انتظار تصاعدي قبل المحاولة التالية
                            await Task.Delay(delayMilliseconds, cancellationToken);
                            delayMilliseconds *= 2;
                        }
                        else
                        {
                            // خطأ قاتل غير قابل للمعالجة
                            if (conversationHistory.Count > 1)
                                conversationHistory.RemoveAt(conversationHistory.Count - 1);

                            return $"فشل الاتصال بـ Groq.\nكود الخطأ: {(int)response.StatusCode}\nالتفاصيل: {responseString}";
                        }
                    }
                }

                return "فشل الاتصال بعد عدة محاولات.";
            }
            catch (OperationCanceledException)
            {
                // ✅ المستخدم أو النظام ألغى الطلب بشكل متعمد
                if (conversationHistory.Count > 1)
                    conversationHistory.RemoveAt(conversationHistory.Count - 1);

                return "تم إلغاء الطلب.";
            }
            catch (System.IO.FileNotFoundException)
            {
                // ✅ ملف المفتاح غير موجود
                return "خطأ: ملف المفتاح 'groq_apikey.txt' غير موجود في مجلد البرنامج.";
            }
            catch (Exception ex)
            {
                return "حدث خطأ غير متوقع: " + ex.Message;
            }
        }

        // ✅ مسح الذاكرة بالكامل (تبدأ محادثة جديدة)
        public void ClearMemory()
        {
            conversationHistory.Clear();
        }

        // ✅ تطبيق IDisposable للتنظيف الصحيح
        public void Dispose()
        {
            // _sharedClient static لا نتخلص منه هنا لأنه مشترك
            conversationHistory?.Clear();
        }
    }
}