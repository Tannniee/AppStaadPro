using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private sealed class WarningScope
        {
            public readonly object Gate = new object();
            public readonly HashSet<string> Keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly object _warningScopesGate = new object();
        private static readonly ConditionalWeakTable<object, WarningScope> _warningScopes =
            new ConditionalWeakTable<object, WarningScope>();
        private static readonly WarningScope _globalWarningScope = new WarningScope();

        private static void WarnOnce(string key, string message, Exception ex = null)
        {
            WarnOnceInScope(_globalWarningScope, key, message, ex);
        }

        private static void WarnOnce(object scopeOwner, string key, string message, Exception ex = null)
        {
            WarnOnceInScope(GetWarningScope(scopeOwner), key, message, ex);
        }

        private static WarningScope GetWarningScope(object scopeOwner)
        {
            if (scopeOwner == null)
                return _globalWarningScope;

            lock (_warningScopesGate)
                return _warningScopes.GetValue(scopeOwner, _ => new WarningScope());
        }

        private static void WarnOnceInScope(WarningScope scope, string key, string message, Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                lock (scope.Gate)
                {
                    if (!scope.Keys.Add(key))
                        return;
                }
            }

            Debug.WriteLine(ex == null ? message : (message + " | " + ex));
            if (ex == null)
                AppLogger.Warning(message);
            else
                AppLogger.Exception(message, ex);
        }

    }
}
