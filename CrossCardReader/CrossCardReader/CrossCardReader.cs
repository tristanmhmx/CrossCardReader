using System;
using System.Threading;
using CrossCardReader.Abstractions;

namespace CrossCardReader.Shared
{
    public class CrossCardReader
    {
        static Lazy<ICardReader> reader = new Lazy<ICardReader>(CreateReader, LazyThreadSafetyMode.PublicationOnly);

        public static ICardReader Current
        {
            get
            {
                ICardReader ret = reader.Value;
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
        internal static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}
