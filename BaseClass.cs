using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP
{
    public class BaseClass
    {
        public void StartInit()
        {
            this.Init();
        }
        public void StartDelete()
        {
            this.Delete();
        }
        virtual public void Init() {
            Log.Info($"initing {this.GetType().FullName}");
        }
        virtual public void Delete() { }
    }
}
