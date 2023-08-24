using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common.Interface
{
    public interface ITransactionListener
    {

        void onSuccess(string paymentResponse);
        void onFailure(string errorMsg, int errorCode);
        void onNext(string action);
    }
}
