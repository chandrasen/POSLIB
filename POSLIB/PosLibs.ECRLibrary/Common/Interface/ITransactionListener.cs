using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common.Interface
{
    public interface ITransactionListener
    {

        void OnSuccess(string paymentResponse);
        void OnFailure(string errorMsg, int errorCode);
        void OnNext(string action);
    }
}
