using System;
using System.Collections.Generic;
using System.Text;

namespace DiscoTranslator.Translation
{
    public class DictTranslationSource : ITranslationSource
    {
        public DictTranslationSource()
        {
            TranslationDictionary = new Dictionary<string, string>();
        }

        public DictTranslationSource(Dictionary<string, string> translationDictionary)
        {
            TranslationDictionary = translationDictionary ?? throw new ArgumentNullException(nameof(translationDictionary));
        }

        public string Name => "DictTranslationSource";

        public bool SourceTranslationAvailable => false;

        public Dictionary<string, string> TranslationDictionary { get; private set; }

        public void Reload() { }

        public bool TryGetTranslation(string Key, out string Translation)
        {
            return TranslationDictionary.TryGetValue(Key, out Translation);
        }
    }
}
