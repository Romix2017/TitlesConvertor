using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TitlesConvertor
{
    public class TitleItem
    {
        public int Id { get; set; }
        public double Length { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public double Pause { get; set; }
        public double FirstItemPause { get; set; }
    }
}
