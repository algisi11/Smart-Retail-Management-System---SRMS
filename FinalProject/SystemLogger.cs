using System;
using System.Data.SqlClient;

namespace FinalProject
{
    public static class SystemLogger
    {
        private static string connectionString = @"Server=.;Database=SmartInventoryDB;Integrated Security=True;";

        /// <summary>
        /// دالة تسجل حركة المستخدم تلقائياً باستخدام SessionManager
        /// </summary>
        /// <param name="actionType">نوع الحركة (مثال: بيع، تعديل، حذف)</param>
        /// <param name="details">التفاصيل الدقيقة لما حدث</param>
        public static void LogAction(string actionType, string details)
        {
            try
            {
                // 🌟 السحر هنا: سحب اسم الموظف تلقائياً من مدير الجلسة
                // وضعنا حماية في حال كان الاسم فارغاً (مثلاً النظام قام بحركة قبل تسجيل الدخول)
                string currentUser = string.IsNullOrEmpty(SessionManager.EmployeeName) ? "النظام" : SessionManager.EmployeeName;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO SystemLogs (Username, ActionType, Details, LogDate) VALUES (@User, @Action, @Details, GETDATE())";
                    SqlCommand cmd = new SqlCommand(query, conn);

                    // تمرير المتغيرات لقاعدة البيانات
                    cmd.Parameters.AddWithValue("@User", currentUser);
                    cmd.Parameters.AddWithValue("@Action", actionType);
                    cmd.Parameters.AddWithValue("@Details", details);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // تجاهل الأخطاء بصمت لكي لا يتعطل عمل الكاشير بسبب خطأ في المراقبة
            }
        }
    }
}