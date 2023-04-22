using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.attribute
{
    internal class RSBaseAttribute : Attribute
    {
        public string Name { get; set; }

        public RSBaseAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
