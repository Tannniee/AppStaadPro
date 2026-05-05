using System;
using System.Reflection;

namespace StaadSteelApp.Core.Interop
{
    internal static class ComInvoke
    {
        internal static object GetProperty(object target, string propertyName, object[] args = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetType().InvokeMember(
                propertyName,
                BindingFlags.GetProperty,
                null,
                target,
                args);
        }

        internal static bool TryGetProperty(object target, string propertyName, out object result, out string error, object[] args = null)
        {
            result = null;
            error = string.Empty;

            try
            {
                result = GetProperty(target, propertyName, args);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static object Invoke(object target, string methodName, object[] args = null, ParameterModifier[] modifiers = null)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return target.GetType().InvokeMember(
                methodName,
                BindingFlags.InvokeMethod,
                null,
                target,
                args,
                modifiers,
                null,
                null);
        }

        internal static bool TryInvoke(object target, string methodName, out object result, out string error, object[] args = null, ParameterModifier[] modifiers = null)
        {
            result = null;
            error = string.Empty;

            try
            {
                result = Invoke(target, methodName, args, modifiers);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                error = ex.InnerException?.Message ?? ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        internal static ParameterModifier[] ByRefModifiers(int parameterCount, params int[] byRefIndexes)
        {
            var modifiers = new ParameterModifier[1];
            modifiers[0] = new ParameterModifier(parameterCount);

            if (byRefIndexes != null)
            {
                foreach (int index in byRefIndexes)
                {
                    if (index >= 0 && index < parameterCount)
                        modifiers[0][index] = true;
                }
            }

            return modifiers;
        }
    }
}
