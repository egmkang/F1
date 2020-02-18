using System;
using System.Collections.Generic;
using System.Text;

namespace F1.Abstractions.Placement
{
    public class PlacementException : Exception
    {
        public PlacementException(int code, string message)  : base(message)
        {
            this.Code = code;
        }

        public int Code { get; set; }
    }
}
