using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StaadSteelApp.Core
{
    public static partial class SupportService
    {
        private static int FindExistingSupportId(
            object supportObj,
            int targetSupportType,
            int[] desiredStates,
            double[] desiredSprings)
        {
            if (!TryGetSupportedNodeIds(supportObj, out int[] supportedNodeIds, out string error))
            {
                Debug.WriteLine("Support scan skipped: " + error);
                return 0;
            }

            var seenSupportIds = new HashSet<int>();
            foreach (int nodeId in supportedNodeIds)
            {
                int assignedType = 0;
                if (targetSupportType != SupportTypeFixedBut &&
                    !TryGetSupportType(supportObj, nodeId, out assignedType, out string typeError))
                {
                    Debug.WriteLine($"Support scan ignored node {nodeId}: {typeError}");
                    continue;
                }

                if (targetSupportType != SupportTypeFixedBut &&
                    !IsMatchingSupportType(assignedType, targetSupportType))
                {
                    continue;
                }

                if (!TryGetSupportDefinition(supportObj, nodeId, out SupportDefinition definition, out string infoError))
                {
                    Debug.WriteLine($"Support scan ignored node {nodeId}: {infoError}");
                    continue;
                }

                if (!seenSupportIds.Add(definition.SupportId))
                    continue;

                if (!IsMatchingSupportType(definition.SupportType, targetSupportType))
                    continue;

                if (targetSupportType == SupportTypeFixedBut &&
                    !IsMatchingFixedButDefinition(definition, desiredStates, desiredSprings))
                {
                    continue;
                }

                return definition.SupportId;
            }

            return 0;
        }

        private static bool IsMatchingSupportType(int existingSupportType, int targetSupportType)
        {
            switch (targetSupportType)
            {
                case SupportTypePinned:
                    return existingSupportType == SupportTypePinned || existingSupportType == SupportTypeGeneratedPinned;
                case SupportTypeFixed:
                    return existingSupportType == SupportTypeFixed || existingSupportType == SupportTypeGeneratedFixed;
                case SupportTypeFixedBut:
                    return existingSupportType == SupportTypeFixedBut || existingSupportType == SupportTypeGeneratedFixedBut;
                default:
                    return existingSupportType == targetSupportType;
            }
        }

        private static bool IsMatchingFixedButDefinition(
            SupportDefinition existing,
            int[] desiredStates,
            double[] desiredSprings)
        {
            if (desiredStates == null || desiredSprings == null)
                return false;

            for (int i = 0; i < 6; i++)
            {
                if (existing.ReleaseStates[i] != desiredStates[i])
                    return false;

                if (Math.Abs(existing.SpringSpec[i] - desiredSprings[i]) > SpringTolerance)
                    return false;
            }

            return true;
        }

        private static bool HasAnyFixedButDefinition(int[] desiredStates, double[] desiredSprings)
        {
            for (int i = 0; i < 6; i++)
            {
                if (desiredStates[i] != 0)
                    return true;

                if (Math.Abs(desiredSprings[i]) > SpringTolerance)
                    return true;
            }

            return false;
        }

        private static int[] NormalizeReleaseSpec(int[] input)
        {
            var result = new int[6];
            if (input == null)
                return result;

            for (int i = 0; i < result.Length && i < input.Length; i++)
                result[i] = input[i] != 0 ? 1 : 0;

            return result;
        }

        private static double[] NormalizeSpringSpec(double[] input)
        {
            var result = new double[6];
            if (input == null)
                return result;

            for (int i = 0; i < result.Length && i < input.Length; i++)
            {
                double value = input[i];
                result[i] = Math.Abs(value) <= SpringTolerance ? 0.0 : value;
            }

            return result;
        }

        private static int[] BuildDesiredSupportStates(int[] releaseSpec, double[] springSpec)
        {
            var states = new int[6];
            for (int i = 0; i < states.Length; i++)
            {
                if (Math.Abs(springSpec[i]) > SpringTolerance)
                {
                    states[i] = -1;
                }
                else
                {
                    states[i] = releaseSpec[i] != 0 ? 1 : 0;
                }
            }

            return states;
        }
    }
}
