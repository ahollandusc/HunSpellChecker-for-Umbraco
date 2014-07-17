These dictionary files are for the HunSpellChecker plugin which uses the .net version http://www.maierhofer.de/en/open-source/nhunspell-net-spell-checker.aspx of HunSpell.

Dictionary files have been sourced primarily from open office extension file format (oxt files).
Additional dictionaries can be found here: http://extensions.openoffice.org/en/search - enter the language and the keyword 'dictionary' !
The .oxt files are actually just .zip files. Rename them and extract the .dic and .aff files.
 
	English AU - http://extensions.openoffice.org/en/project/english-australian-dictionary  (pretty much GB english + a few Australianisms...)
	US - http://extensions.openoffice.org/en/project/us-english-spell-checking-dictionary
	Danish - http://extensions.services.openoffice.org/project/dict-da
	Dutch - http://extensions.services.openoffice.org/project/dict-nl
	Finnish - not supported by Hunspell...
	French - http://extensions.openoffice.org/en/project/french-dictionaries-%E2%80%94-dictionnaires-fran%C3%A7ais
	German - http://extensions.services.openoffice.org/project/dict-de_DE_frami
	Italian - http://prdownloads.sf.net/linguistico/Dizionari.IT_20081129.oxt?download
	Polish - http://extensions.openoffice.org/en/project/polish-dictionary-pack
	Portuguese - http://extensions.openoffice.org/en/project/vero-brazilian-portuguese-spellchecking-dictionary-hyphenator
	Spanish - http://extensions.openoffice.org/en/project/spanish-espa%C3%B1ol
	Swedish - http://extensions.openoffice.org/en/project/swedish-dictionaries-apache-openoffice

Languages are loaded based on the current locale or by the tinymce configured 2 digit language code.

To load a language, the following files must be present in the 'dict' subfolder and use the naming convention as described here http://www.maierhofer.de/en/open-source/hunspell-dictionaries.aspx

	eg en_us.dic, en_usc.aff

	Note the '_' underscores!

To reduce the memory footprint and site startup times, dictionary loading can be controlled by the HunSpellCheckerStartupMode appSetting in the web.config
Valid options are:
    LoadNone       // Dont load any languages at startup - only load on demand.
    LoadCurrent    // Load only the language for the current locale - others are loaded on demand.
    LoadAll        // Load all installed languages at startup.
