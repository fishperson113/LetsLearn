using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsLearn.UseCases.DTOs
{
    public class SuccessResponseDTO
    {
        public string Message { get; set; }

        public SuccessResponseDTO(string message)
        {
            Message = message;
        }
    }
}
