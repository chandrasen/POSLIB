using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common.Interface
{
    public interface IConnectionListener
    {
        void OnFailure(string message);
        void OnSuccess(string message);
    }
}
