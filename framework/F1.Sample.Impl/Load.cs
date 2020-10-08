using System;
using System.Collections.Generic;
using System.Text;
using Sample;

namespace F1.Sample.Impl
{
    public class Load
    {
        /// <summary>
        /// 暂时先不处理这个, 强制加载dll
        /// </summary>
        public static void LoadByForce() 
        {
            var _ = new ResponseLogin();
        }
    }
}
