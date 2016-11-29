using System;

namespace CrossCardReader.Abstractions
{
    /// <summary>
    /// Card Model
    /// </summary>
    public class Card
    {
        /// <summary>
        /// Card Number
        /// </summary>
        public Int64 Number { get; set; }
        /// <summary>
        /// Expiration Month
        /// </summary>
        public int Month { get; set; }
        /// <summary>
        /// Expiration Year
        /// </summary>
        public int Year { get; set; }
    }
}
