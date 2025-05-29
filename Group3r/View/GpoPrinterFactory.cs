﻿using Group3r.Options;

namespace Group3r.View
{
    /**
     * Summary: Factory to create GpoPrinter given the control flag.
     */
    static class GpoPrinterFactory
    {
        /**
         * Summary: Returns a GpoPrinter given the setting.
         * Arguments: string containing the control flag.
         * Returns: IGpoPrinter instance.
         */
        public static IGpoPrinter GetPrinter(string setting, GrouperOptions options)
        {
            IGpoPrinter processor;
            
            switch (setting?.ToLower())
            {
                case "json":
                    processor = new JsonGpoPrinter(options);
                    break;
                case "nice":
                    processor = new NiceGpoPrinter(options);
                    break;
                default:
                    processor = new NiceGpoPrinter(options);
                    break;
            }

            return processor;
        }
    }
}
