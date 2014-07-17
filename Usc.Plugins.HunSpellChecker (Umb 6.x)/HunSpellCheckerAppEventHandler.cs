using System.Threading;
using Umbraco.Core;
using Umbraco.Web;

namespace Usc.Plugins.HunSpellChecker
{
    public class HunSpellCheckerAppEventHandler : IApplicationEventHandler
    {
        // Guarantee we are starting up.
        private static readonly object _lockObj = new object();
        private static bool _ran = false; 

        public void OnApplicationInitialized(UmbracoApplicationBase httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplicationBase httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarted(UmbracoApplicationBase httpApplication, ApplicationContext applicationContext)
        {
            // lock
            if (!_ran)
            {
                lock (_lockObj)
                {
                    if (!_ran)
                    {
                        // everything we do here is blocking on application start, so we should be quick. 
                        // do you're registering here... or in a function 

                        Thread loader = new Thread(HunSpellChecker.Initialise);
                        loader.Priority = ThreadPriority.Lowest;
                        loader.Start();

                        _ran = true;
                    }
                }
            }
        }
    }
}