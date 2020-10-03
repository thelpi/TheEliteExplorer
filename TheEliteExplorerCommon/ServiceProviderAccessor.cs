using System;

namespace TheEliteExplorerCommon
{
    /// <summary>
    /// Global access to non-injectable services.
    /// </summary>
    public static class ServiceProviderAccessor
    {
        private static IServiceProvider _provider = null;

        /// <summary>
        /// Sets the provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <exception cref="InvalidOperationException">The provider is already defined.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <c>Null</c>.</exception>
        public static void SetProvider(IServiceProvider provider)
        {
            if (_provider != null)
            {
                throw new InvalidOperationException("The provider is already defined.");
            }

            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Clock provider.
        /// </summary>
        /// <exception cref="InvalidOperationException">The provider is not defined.</exception>
        public static IClockProvider ClockProvider
        {
            get
            {
                if (_provider == null)
                {
                    throw new InvalidOperationException("The provider is not defined.");
                }

                return (IClockProvider)_provider.GetService(typeof(IClockProvider));
            }
        }
    }
}
