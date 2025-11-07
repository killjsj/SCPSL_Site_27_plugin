using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    public abstract class BaseClass
    {
        public void StartInit()
        {
            Log.Info($"initing {this.GetType().FullName}");
            this.Init();
        }
        public void StartDelete()
        {
            Log.Info($"deleting {this.GetType().FullName}");
            this.Delete();
        }
        abstract public void Init();
        abstract public void Delete();
    }
}
