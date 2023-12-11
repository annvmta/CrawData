using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrawData.DesignPattern
{
    public static class CrawPattern
    {
        /// <summary>
        /// chạy lại fun
        /// </summary>
        /// <param name="fun">fun cần chạy</param>
        /// <param name="retryCount">số lần chạy</param>
        /// <param name="waitTime">thời gian giữa 2 lần chạy lại</param>
        /// <returns></returns>
        public static async Task<T> ReTry<T>(Func<Task<T>> fun, int retryCount = 2, int waitTime = 1500)
        {
            int i = 0;
            for (i = 0; i < retryCount; i++)
            {
                try
                {
                    return await fun();
                }
                catch (Exception ex)
                {
                    if(ex is InvalidCastException) 
                    throw ex;
                    Console.Write($"ReTry {i + 1}, Exc: {ex.Message}");
                    await Task.Delay(waitTime);
                }
            }
            return await fun();
        }
    }
}
