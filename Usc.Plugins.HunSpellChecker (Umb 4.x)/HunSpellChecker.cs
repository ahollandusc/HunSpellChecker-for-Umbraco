using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using log4net;
using NHunspell;
using umbraco.presentation.umbraco_client.tinymce3.plugins.spellchecker;

namespace Usc.Plugins.HunSpellChecker
{
    /// <summary>
    /// Class built to use Hun SpellChecker
    /// See http://www.nuget.org/packages/NHunspell/
    /// Additional dictionaries can be found here: http://extensions.openoffice.org/en/search - enter the language and the keyword dictionary!
    /// 
    /// </summary>
    public class HunSpellChecker : SpellChecker, IHttpHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static string _dictionaryPath;
        private static bool _initComplete = false;
        private static SpellEngine _spellEngine;

        /// <summary>
        /// Determines which languages are initialised at app startup.
        /// </summary>
        private enum StartupModes
        {
            Disabled,       // Dont load the spellchecker at all.
            LoadNone,       // Dont load any languages at startup - only load on demmand.
            LoadCurrent,    // Load only the language for the current locale.
            LoadAll         // Load all languages that are installed.
        }

        private static string DefaultDictionaryCulture
        {
            get
            {
                string dictCulture = ConfigurationManager.AppSettings["HunSpellCheckerDefaultDictionaryCulture"];
                return (!string.IsNullOrEmpty(dictCulture)) ? dictCulture : Thread.CurrentThread.CurrentCulture.Name;
            }
        }

        private static StartupModes StartupMode
        {
            get
            {
                StartupModes mode;
                string modeSetting = ConfigurationManager.AppSettings["HunSpellCheckerStartupMode"];
                if (!string.IsNullOrEmpty(modeSetting))
                {
                    if (!Enum.TryParse(modeSetting, true, out mode))
                    {
                        log.Info("Invalid HunSpellCheckerStartupMode mode setting " + modeSetting +
                                 ". Defaulting to 'Current' startup mode.");
                        mode = StartupModes.LoadCurrent;
                    }
                }
                else
                {
                    mode = StartupModes.LoadCurrent;
                }

                return mode;
            }
        }

        private static SpellFactory SpellEngine(string language)
        {
            if (_spellEngine == null || !_initComplete)
            {
                log.Info("Spell checker accessed before Init Completed.");
                throw new Exception("Spell checker plugin not yet initialised. Please try again later.");
            }

            SpellFactory result;

            try
            {
                result = _spellEngine[language];
            }
            catch (KeyNotFoundException)
            {
                // Language not found - try and load it.
                try
                {
                    LoadDictionaryByLanguage(language);
                    result = _spellEngine[language];
                }
                catch (Exception ex)
                {
                    string message = "Error loading spell check language '" + language +
                                     "'. Language has not been installed. " + ex.Message;
                    log.Error(message, ex);
                    throw new Exception(message);
                }
            }

            return result;
        }

        public static void Initialise()
        {
            if (StartupMode == StartupModes.Disabled) return;

            try
            {
                // Setup paths
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
//                Hunspell.NativeDllPath = Path.Combine(basePath, "bin");
                _dictionaryPath = Path.Combine(basePath, "umbraco\\plugins\\HunSpellChecker\\dict");

                // Copy all dictionary files into one directory.
                // At app startup, load dictionary based on current culture.  en-au   or    match    en-* if no en-AU
                // At request time if dont have language loaded, load based on first matching lang eg en --> en_AU
                // da --> da_DK

                _spellEngine = new SpellEngine();

                switch (StartupMode)
                {
                    case StartupModes.LoadAll:
                        LoadAllDictionaries();
                        log.Info(StartupMode + " - Loaded all dictionaries.");
                        break;
                    case StartupModes.LoadNone:
                        log.Info(StartupMode + " - Skipping dictionary load.");
                        // dont load any
                        break;
                    default: // assume StartupModes.Current
                        LoadDictionaryByCulture(DefaultDictionaryCulture);
                        log.Info(StartupMode + " - Loaded default dictionary culture: " + DefaultDictionaryCulture);
                        break;
                }

                _initComplete = true;
            }
            catch (Exception ex)
            {
                log.Error("Error occurred initialising spell checker: " + ex.Message, ex);
                if (_spellEngine != null)
                    _spellEngine.Dispose();
            }
        }

        public static void Dispose()
        {
            if (_spellEngine != null) 
            {
                _spellEngine.Dispose();
                _spellEngine = null;
            }
        }

        /// <summary>
        /// Load all dictionaries that have been installed, starting with the current culture dictionary.
        /// Dictionary files are installed to the plugins\HunSpellChecker\dict folder.
        /// There should be a .dic file and a .aff file for each culture installed. eg
        /// 
        ///   en_AU.dic, en_AU.aff
        /// 
        /// </summary>
        private static void LoadAllDictionaries()
        {
            LoadDictionaryByCulture(DefaultDictionaryCulture);

            // Now load the remaining dictionaries that are installed.

            DirectoryInfo dirInfo = new DirectoryInfo(_dictionaryPath);
            FileInfo[] dictFiles = dirInfo.GetFiles("*.dic");

            foreach (FileInfo dictFile in dictFiles)
            {
                string culture = Path.GetFileNameWithoutExtension(dictFile.Name);
                string language = culture.Substring(0, 2);

                try
                {
                    try
                    {
                        SpellFactory ignored = _spellEngine[language];
                    }
                    catch (KeyNotFoundException)
                    {
                        // Language of this culture not found - try and load it.
                        LoadDictionaryByCulture(culture);
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error occurred loading language for culture '" + culture + "' - " + ex.Message, ex);
                }
            }
        }

        /// <summary>
        /// Loads the dictionary that best matches the specified language.
        /// </summary>
        /// <param name="language">A 2 character language 'en' or a culture</param>
        private static void LoadDictionaryByLanguage(string language)
        {
            // If this is the language for the current locale then use the current culture. (stops us acidentally loading AU culture for US) 
            if (DefaultDictionaryCulture.Substring(0, 2)
                      .Equals(language, StringComparison.CurrentCultureIgnoreCase))
            {
                LoadDictionaryByCulture(DefaultDictionaryCulture);
            }
            else
            {

                // Look for dictionary files with a culture that matches this language.
                DirectoryInfo dirInfo = new DirectoryInfo(_dictionaryPath);
                FileInfo[] languageFiles = dirInfo.GetFiles(language + "*.*");
                if (languageFiles.Length == 0)
                {
                    throw new Exception("No .dic dictionary files for language '" + language + "' were found in HunSpellChecker\\dict folder");
                }

                string culture = Path.GetFileNameWithoutExtension(languageFiles[0].Name);

                LoadDictionaryByCulture(culture);
            }
        }

        /// <summary>
        /// Loads the dictionary for the specified culture.
        /// </summary>
        /// <param name="culture">A culture name - eg 'en-AU'</param>
        private static void LoadDictionaryByCulture(string culture)
        {
            LanguageConfig enConfig = new LanguageConfig();
            enConfig.LanguageCode = culture.Substring(0, 2);

            string filePrefix = culture.Replace("-", "_");  // language files have an _ not a -

            enConfig.HunspellAffFile = Path.Combine(_dictionaryPath, filePrefix + ".aff");
            enConfig.HunspellDictFile = Path.Combine(_dictionaryPath, filePrefix + ".dic");

            _spellEngine.AddLanguage(enConfig);

            log.Info("Culture '" + culture + "' loaded for spellcheck plugin.");
        }

        /// <summary>
        /// Checks all the words submitted with the HunSpellChecker.
        /// </summary>
        /// <param name="language">The language code.</param>
        /// <param name="words">The words to be checked.</param>
        /// <returns></returns>
        public override SpellCheckerResult CheckWords(string language, string[] words)
        {
            var scr = new SpellCheckerResult();
            var spellChecker = SpellEngine(language);

            foreach (string word in words)
            {
                bool correct = spellChecker.Spell(word);
                if (!correct)
                {
                    scr.result.Add(word);
                }
            }

            return scr;
        }

        /// <summary>
        /// Gets the suggested spelling for a misspelt word
        /// </summary>
        /// <param name="language">The language the word is.</param>
        /// <param name="word">The word that is misspelt.</param>
        /// <returns></returns>
        public override SpellCheckerResult GetSuggestions(string language, string word)
        {
            var scr = new SpellCheckerResult();
            var spellChecker = SpellEngine(language);

            scr.result = spellChecker.Suggest(word);
            return scr;
        }

        #region IHttpHandler Members

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            SpellCheckerInput input = SpellCheckerInput.Parse(new StreamReader(context.Request.InputStream));
            SpellCheckerResult suggestions = null;
            switch (input.Method)
            {
                case "checkWords":
                    suggestions = CheckWords(input.Language, input.Words.ToArray());
                    break;

                case "getSuggestions":
                    suggestions = GetSuggestions(input.Language, input.Words[0]);
                    break;

                default:
                    suggestions = new SpellCheckerResult();
                    break;
            }

            suggestions.id = input.Id;
            JavaScriptSerializer ser = new JavaScriptSerializer();
            var res = ser.Serialize(suggestions);
            context.Response.Write(res);
        }

        #endregion
    }

}