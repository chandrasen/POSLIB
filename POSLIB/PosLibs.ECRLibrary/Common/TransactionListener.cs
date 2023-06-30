using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public interface TransactionListener
    {

        void onSuccess(String paymentResponse);
        void onFailure(string errorMsg, int errorCode);
        void onNext(string action);
    }
}
