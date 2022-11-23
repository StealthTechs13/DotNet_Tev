using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tev.API.Models
{
    public class TextValueResponse<T>
    {
        /// <summary>
        /// Display text, to be used for UI display
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Value, to be used for API communications
        /// </summary>
        public T Value { get; set; }
    }
}
