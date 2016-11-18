using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCardReader.Abstractions
{
    public interface ICardReader
    {
        Task<bool> Initialize(string apiKey);
        Task<Card> RecognizeCardAsync(Products supportedProducts);
    }
}
