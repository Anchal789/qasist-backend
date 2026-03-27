using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QAsist.Application.Common.Exceptions
{
    public class OpenApiValidationException : BadRequestException
    {
        public OpenApiValidationException(string message)
            : base(message)
        {
        }
    }
}
