using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Interfaces {
    public interface Service {
        public void CancelService(); //cleanup and close service
    }
}
