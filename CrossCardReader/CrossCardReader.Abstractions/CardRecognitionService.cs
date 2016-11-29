using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace CrossCardReader.Abstractions
{
    /// <summary>
    /// Service for Card Recognition
    /// </summary>
    public class CardRecognitionService
    {
        #region Validation Parameters

        private static string[] PreferredCardRole { get; set; }
        #endregion

        private readonly OcrRecognitionService service;

        /// <summary>
        /// Initialize Service with products and cognitive api key
        /// </summary>
        /// <param name="products"></param>
        /// <param name="apiKey"></param>
        public CardRecognitionService(string[] products, string apiKey)
        {
            service = new OcrRecognitionService();
            service.Init(apiKey);
            PreferredCardRole = products;
        }
        /// <summary>
        /// Recognized a Card
        /// </summary>
        /// <param name="data">Image</param>
        /// <returns>Card</returns>
        public async Task<Card> RecognizeCard(byte[] data)
        {
            var card = new Card();

            var results = await service.RecognizeAndObject(data);

            if (results.Regions == null) return card;

            foreach (var region in results.Regions)
            {
                var cardn = string.Empty;

                var expiry = string.Empty;

                var cardLine = false;

                foreach (var line in region.Lines)
                {
                    var exp = line.Words.FirstOrDefault(w => w.Text.Contains("/"));
                    if (exp?.Text.Length == 5)
                    {
                        expiry = exp.Text;
                        continue;
                    }
                    if (cardn.Length == 4)
                        break;
                    if (!(line.Words?.Length > 2)) continue;

                    foreach (var word in line.Words)
                    {
                        cardLine = PreferredCardRole.Any(k => k.StartsWith(word.Text));
                        if (cardLine) break;
                    }
                    if (cardLine)
                    {
                        cardn = line.Words.Aggregate(cardn, (current, word) => current + word.Text);
                    }
                }
                if (!string.IsNullOrEmpty(cardn))
                {
                    card.Number = Convert.ToInt64(cardn);
                }
                if (string.IsNullOrEmpty(expiry)) continue;
                card.Month = Convert.ToInt32(expiry.Substring(0, 2));
                card.Year = Convert.ToInt32(expiry.Substring(3, 2));
            }
            return card;
        }

    }
    /// <summary>
    /// Cognitive service OCR
    /// </summary>
    public class OcrRecognitionService
    {
        #region Fields
        /// <summary>
        /// Api Key for Microsoft Face Vision Api
        /// </summary>
        private static string ApiKey { get; set; }

        /// <summary>
        /// Sets if the class should throw errors
        /// </summary>
        private static bool ThrowErrors { get; set; }

        #endregion Fields

        #region Constructors

        /// <summary>
        /// You must initialize this service in order to be used, otherwise will throw an error
        /// </summary>
        /// <param name="apiKey">Key of Project Oxford</param>
        /// <param name="breakOnNoText">Flag return if no text is found</param>
        /// <param name="throwErrors">Flag</param>
        public void Init(string apiKey, bool breakOnNoText = true, bool throwErrors = true)
        {
            ApiKey = apiKey;
            ThrowErrors = throwErrors;
        }
        #endregion Constructors

        /// <summary>
        /// Sends bytes to Project Oxford and performs OCR
        /// </summary>
        /// <param name="data">The byte array of image</param>
        /// <param name="language">The language code to recognize for</param>
        /// <returns></returns>
        private static async Task<OcrResults> UploadAndRecognizeImage(byte[] data, string language)
        {
            var visionServiceClient = new VisionServiceClient(ApiKey);
            using (Stream imageStream = new MemoryStream(data))
            {
                var ocrResult = await visionServiceClient.RecognizeTextAsync(imageStream, language);
                return ocrResult;
            }
        }

        /// <summary>
        /// Perform ocr recognition oriented to custom objects
        /// </summary>
        /// <param name="image">Image Bytes to Scan</param>
        /// <returns></returns>
        internal async Task<OcrResults> RecognizeAndObject(byte[] image)
        {
            try
            {
                var languageCode = GetSupportedLanguages().First().ShortCode;

                var ocrResult = await UploadAndRecognizeImage(image, languageCode);

                return ocrResult;
            }
            catch (Exception ex)
            {
                if (ThrowErrors)
                    throw new Exception($"Response: {ex.HResult}. {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<RecognizeLanguage> GetSupportedLanguages()
        {
            return new List<RecognizeLanguage>
            {
                new RecognizeLanguage{ ShortCode = "unk",     LongName = "AutoDetect"  },
                new RecognizeLanguage{ ShortCode = "ar",      LongName = "Arabic"  },
                new RecognizeLanguage{ ShortCode = "zh-Hans", LongName = "Chinese (Simplified)"  },
                new RecognizeLanguage{ ShortCode = "zh-Hant", LongName = "Chinese (Traditional)"  },
                new RecognizeLanguage{ ShortCode = "cs",      LongName = "Czech"  },
                new RecognizeLanguage{ ShortCode = "da",      LongName = "Danish"  },
                new RecognizeLanguage{ ShortCode = "nl",      LongName = "Dutch"  },
                new RecognizeLanguage{ ShortCode = "en",      LongName = "English"  },
                new RecognizeLanguage{ ShortCode = "fi",      LongName = "Finnish"  },
                new RecognizeLanguage{ ShortCode = "fr",      LongName = "French"  },
                new RecognizeLanguage{ ShortCode = "de",      LongName = "German"  },
                new RecognizeLanguage{ ShortCode = "el",      LongName = "Greek"  },
                new RecognizeLanguage{ ShortCode = "hu",      LongName = "Hungarian"  },
                new RecognizeLanguage{ ShortCode = "it",      LongName = "Italian"  },
                new RecognizeLanguage{ ShortCode = "ja",      LongName = "Japanese"  },
                new RecognizeLanguage{ ShortCode = "ko",      LongName = "Korean"  },
                new RecognizeLanguage{ ShortCode = "nb",      LongName = "Norwegian"  },
                new RecognizeLanguage{ ShortCode = "pl",      LongName = "Polish"  },
                new RecognizeLanguage{ ShortCode = "pt",      LongName = "Portuguese"  },
                new RecognizeLanguage{ ShortCode = "ro",      LongName = "Romanian" },
                new RecognizeLanguage{ ShortCode = "ru",      LongName = "Russian"  },
                new RecognizeLanguage{ ShortCode = "sr-Cyrl", LongName = "Serbian (Cyrillic)" },
                new RecognizeLanguage{ ShortCode = "sr-Latn", LongName = "Serbian (Latin)" },
                new RecognizeLanguage{ ShortCode = "sk",      LongName = "Slovak" },
                new RecognizeLanguage{ ShortCode = "es",      LongName = "Spanish"  },
                new RecognizeLanguage{ ShortCode = "sv",      LongName = "Swedish"  },
                new RecognizeLanguage{ ShortCode = "tr",      LongName = "Turkish"  }
            };
        }
    }

    internal class RecognizeLanguage
    {
        public string ShortCode { get; set; }
        public string LongName { get; set; }
    }
}
