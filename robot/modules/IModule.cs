using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.robot.modules
{
    internal interface IModule
    {
        public static string StaticName { get; }
        public string ModuleName { get; }
        public abstract RS.Snail.JJJ.boot.Context _context { get; }

        public bool Inited { get; }
        public void Init(bool load = false);
    }
}
