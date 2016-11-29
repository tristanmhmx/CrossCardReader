using System;
using CrossCardReader.Abstractions;

namespace CrossCardReader
{
    /// <summary>
    /// Card Reader for non-setoff cards
    /// </summary>
    public class CrossCardReader
    {
        static Lazy<ICardReader> Implementation = new Lazy<ICardReader>(() => CreateReader(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current instance of the reader
        /// </summary>
        /// <exception cref="Exception">Throw exception if reader is null</exception>
        public static ICardReader Current
        {
            get
            {
                ICardReader ret = Implementation.Value;
                if (ret == null) throw NotImplementedInReferenceAssembly();
                return ret;
            }
        }

        static ICardReader CreateReader()
        {
#if PORTABLE
            return null;
#else
            return new ReaderImplementation();
#endif
        }

        private static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}
