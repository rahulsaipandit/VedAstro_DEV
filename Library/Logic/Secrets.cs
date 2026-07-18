using System;
using System.Collections.Generic;
using System.Reflection;


namespace VedAstro.Library
{

    /// <summary>
    /// OBS SECURITY PROTOCOL
    /// </summary>
    public static partial class Secrets
    {
        /// <summary>
        /// OBS SECURITY PROTOCOL
        /// </summary>
        public static string Get(string key)
        {
            //keys are expected to be in private mode, accessed only via this method
            var field = typeof(Secrets).GetField(key, BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                return (string)field.GetValue(null);
            }

            Console.WriteLine($"The key --> '{key}' is missing sweetheart! Contact us for a testing Key --> vedastro.org/Contact.html");
            //give nice message to caller if missing
            throw new Exception($"The key --> '{key}' is missing sweetheart! Contact us for a testing Key --> vedastro.org/Contact.html");
            return "";
        }

        /// <summary>
        /// Same lookup as Get(), but returns null instead of throwing when the key is missing.
        /// For call sites where a missing cloud key should degrade gracefully (e.g. local dev routes
        /// the request to a local LLM before the key is ever used) rather than crash the caller.
        /// </summary>
        public static string? TryGet(string key)
        {
            var field = typeof(Secrets).GetField(key, BindingFlags.Static | BindingFlags.NonPublic);
            return field != null ? (string)field.GetValue(null) : null;
        }
    }
}
