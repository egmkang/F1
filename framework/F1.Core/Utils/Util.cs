using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace F1.Core.Utils
{
    public static class Util
    {
        public static async Task RunTaskTimer(Func<Task> f, int interval) 
        {
            long beginMilliSeconds = Platform.GetMilliSeconds();
            long count = 0;
            while (true) 
            {
                count++;

                try
                {
                    await f().ConfigureAwait(false);
                }
                catch (Exception e) 
                {
                }

                var sleep = beginMilliSeconds + count * interval - Platform.GetMilliSeconds();
                if (sleep > 0) 
                {
                    await Task.Delay((int)sleep).ConfigureAwait(false);
                }
            }
        }
        public static async Task RunTaskTimer(Action f, int interval) 
        {
            long beginMilliSeconds = Platform.GetMilliSeconds();
            long count = 0;
            while (true) 
            {
                count++;

                try
                {
                    f();
                }
                catch (Exception e) 
                {
                }

                var sleep = beginMilliSeconds + count * interval - Platform.GetMilliSeconds();
                if (sleep > 0) 
                {
                    await Task.Delay((int)sleep).ConfigureAwait(false);
                }
            }
        }
    }
}
