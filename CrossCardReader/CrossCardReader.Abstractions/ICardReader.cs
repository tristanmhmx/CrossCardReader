using System;
using System.Threading.Tasks;

namespace CrossCardReader.Abstractions
{
    /// <summary>
    /// Read a card without card number setoff from a cross platform API.
    /// </summary>
    public interface ICardReader
    {
        /// <summary>
        /// Property to check if camera is available
        /// </summary>
        bool IsCameraAvailable { get; }
        /// <summary>
        /// Initializes control and sets Cognitive Api Key
        /// </summary>
        /// <param name="apiKey">Cognitvie Services Api Key</param>
        /// <returns>True if initialized</returns>
        Task<bool> Initialize(string apiKey);
        /// <summary>
        /// Recognizes a card given a list of supportes bins (8 digits)
        /// </summary>
        /// <param name="supportedProducts">Hashset of supported bins</param>
        /// <returns>Card number and expiration</returns>
        /// <exception cref="NotSupportedException">Exception thrown if camera is not available, supported products is null or hashset count is 0</exception>
        Task<Card> RecognizeCardAsync(Products supportedProducts);
    }
}
