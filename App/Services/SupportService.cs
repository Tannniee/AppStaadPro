using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using StaadSteelApp;
using StaadSteelApp.Core.Interop;
using StaadSteelApp.Core.Misc;

namespace StaadSteelApp.Core
{
    public static class SupportService
    {
        private const int SupportTypePinned = 1;
        private const int SupportTypeFixed = 2;
        private const int SupportTypeFixedBut = 3;
        private const int SupportTypeGeneratedPinned = 11;
        private const int SupportTypeGeneratedFixed = 12;
        private const int SupportTypeGeneratedFixedBut = 13;
        private const double SpringTolerance = 1e-9;

        public static void ApplyFixed_Selected()
        {
            ApplySimpleSupportToSelectedNodes(
                supportType: SupportTypeFixed,
                title: "Support");
        }

        public static void ApplyPinned_Selected()
        {
            ApplySimpleSupportToSelectedNodes(
                supportType: SupportTypePinned,
                title: "Support");
        }

        public static void RemoveSupport_Selected()
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            if (!TryGetSelectedNodeIds(out int[] nodeIds, out string selectionMessage))
            {
                TopMostMessageBox.ShowInfo(selectionMessage, "Support");
                return;
            }

            object openStaad = os;
            if (!OpenStaadSupportAdapter.TryGetSupportObject(openStaad, out object supportObj, out string supportError))
            {
                TopMostMessageBox.ShowError(supportError, "Support");
                return;
            }

            int removedCount = 0;
            var errors = new StringBuilder();

            foreach (int nodeId in nodeIds)
            {
                if (!TryGetSupportType(supportObj, nodeId, out int supportType, out string typeError))
                {
                    AppendNodeError(errors, nodeId, typeError);
                    continue;
                }

                if (supportType == 0)
                    continue;

                if (TryRemoveSupportFromNode(supportObj, nodeId, out string removeError))
                {
                    removedCount++;
                }
                else
                {
                    AppendNodeError(errors, nodeId, removeError);
                }
            }

            ShowNodeOperationResult(
                actionName: "Support",
                successMessage: $"Support removed from {removedCount}/{nodeIds.Length} node(s).",
                noOpMessage: "Selected nodes do not have any support assigned.",
                removedOrAssignedCount: removedCount,
                totalCount: nodeIds.Length,
                errors: errors);
        }

        public static void ApplyFixedBut_Selected(int[] releaseSpec, double[] springSpec)
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            if (!TryGetSelectedNodeIds(out int[] nodeIds, out string selectionMessage))
            {
                TopMostMessageBox.ShowInfo(selectionMessage, "Support");
                return;
            }

            int[] normalizedRelease = NormalizeReleaseSpec(releaseSpec);
            double[] normalizedSpring = NormalizeSpringSpec(springSpec);
            int[] desiredStates = BuildDesiredSupportStates(normalizedRelease, normalizedSpring);

            if (!HasAnyFixedButDefinition(desiredStates, normalizedSpring))
            {
                TopMostMessageBox.ShowWarning(
                    "Please define at least one release or spring stiffness.",
                    "Fixed But");
                return;
            }

            object openStaad = os;
            if (!OpenStaadSupportAdapter.TryGetSupportObject(openStaad, out object supportObj, out string supportError))
            {
                TopMostMessageBox.ShowError(supportError, "Fixed But");
                return;
            }

            int supportId = FindExistingSupportId(
                supportObj,
                targetSupportType: SupportTypeFixedBut,
                desiredStates,
                normalizedSpring);

            bool reusedExisting = supportId > 0;
            if (!reusedExisting)
            {
                supportId = CreateSupportFixedBut(supportObj, normalizedRelease, normalizedSpring, out string createError);
                if (supportId <= 0)
                {
                    string message = string.IsNullOrWhiteSpace(createError)
                        ? "Could not create Fixed But support."
                        : "Could not create Fixed But support: " + createError;
                    TopMostMessageBox.ShowError(message, "Fixed But");
                    return;
                }
            }

            AssignSupportAndReport(
                supportObj,
                nodeIds,
                supportId,
                successTitle: "Fixed But",
                successVerb: "Fixed But applied",
                reusedExisting: reusedExisting);
        }

        private static void ApplySimpleSupportToSelectedNodes(int supportType, string title)
        {
            dynamic os = StaadManager.GetOpenSTAAD();
            if (os == null) return;

            if (!TryGetSelectedNodeIds(out int[] nodeIds, out string selectionMessage))
            {
                TopMostMessageBox.ShowInfo(selectionMessage, title);
                return;
            }

            object openStaad = os;
            if (!OpenStaadSupportAdapter.TryGetSupportObject(openStaad, out object supportObj, out string supportError))
            {
                TopMostMessageBox.ShowError(supportError, title);
                return;
            }

            int supportId = FindExistingSupportId(supportObj, supportType, desiredStates: null, desiredSprings: null);
            bool reusedExisting = supportId > 0;

            if (!reusedExisting)
            {
                supportId = CreateSimpleSupport(supportObj, supportType, out string createError);
                if (supportId <= 0)
                {
                    string kind = supportType == SupportTypeFixed ? "Fixed" : "Pinned";
                    string message = string.IsNullOrWhiteSpace(createError)
                        ? $"Could not create {kind} support."
                        : $"Could not create {kind} support: {createError}";
                    TopMostMessageBox.ShowError(message, title);
                    return;
                }
            }

            AssignSupportAndReport(
                supportObj,
                nodeIds,
                supportId,
                successTitle: title,
                successVerb: supportType == SupportTypeFixed ? "Fixed applied" : "Pinned applied",
                reusedExisting: reusedExisting);
        }

        private static void AssignSupportAndReport(
            object supportObj,
            int[] nodeIds,
            int supportId,
            string successTitle,
            string successVerb,
            bool reusedExisting)
        {
            int assignedCount = 0;
            var errors = new StringBuilder();

            foreach (int nodeId in nodeIds)
            {
                if (TryAssignSupportToNode(supportObj, nodeId, supportId, out string assignError))
                {
                    assignedCount++;
                }
                else
                {
                    AppendNodeError(errors, nodeId, assignError);
                }
            }

            string origin = reusedExisting ? "reused existing support" : "created new support";
            string successMessage = $"{successVerb} to {assignedCount}/{nodeIds.Length} node(s) ({origin} #{supportId}).";
            ShowNodeOperationResult(
                actionName: successTitle,
                successMessage: successMessage,
                noOpMessage: "No node was updated.",
                removedOrAssignedCount: assignedCount,
                totalCount: nodeIds.Length,
                errors: errors);
        }

        private static void ShowNodeOperationResult(
            string actionName,
            string successMessage,
            string noOpMessage,
            int removedOrAssignedCount,
            int totalCount,
            StringBuilder errors)
        {
            string errorText = errors.ToString().Trim();
            if (removedOrAssignedCount == totalCount && string.IsNullOrEmpty(errorText))
            {
                TopMostMessageBox.ShowSuccess(successMessage, actionName);
                return;
            }

            if (removedOrAssignedCount <= 0 && string.IsNullOrEmpty(errorText))
            {
                TopMostMessageBox.ShowInfo(noOpMessage, actionName);
                return;
            }

            var message = new StringBuilder();
            if (removedOrAssignedCount > 0)
                message.AppendLine(successMessage);
            if (!string.IsNullOrEmpty(errorText))
                message.Append(errorText);

            TopMostMessageBox.ShowWarning(message.ToString().Trim(), actionName);
        }

        private static bool TryGetSelectedNodeIds(out int[] nodeIds, out string message)
        {
            return MiscSelectionService.TryGetSelectedNodeIds(out nodeIds, out message);
        }

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

        private static int CreateSimpleSupport(object supportObj, int supportType, out string error)
        {
            string methodName = supportType == SupportTypeFixed
                ? "CreateSupportFixed"
                : "CreateSupportPinned";

            return supportType == SupportTypeFixed
                ? OpenStaadSupportAdapter.CreateSupportFixed(supportObj, out error)
                : OpenStaadSupportAdapter.CreateSupportPinned(supportObj, out error);
        }

        private static int CreateSupportFixedBut(object supportObj, int[] releaseSpec, double[] springSpec, out string error)
        {
            return OpenStaadSupportAdapter.CreateSupportFixedBut(supportObj, releaseSpec, springSpec, out error);
        }

        private static bool TryAssignSupportToNode(object supportObj, int nodeId, int supportId, out string error)
        {
            return OpenStaadSupportAdapter.TryAssignSupportToNode(supportObj, nodeId, supportId, out error);
        }

        private static bool TryRemoveSupportFromNode(object supportObj, int nodeId, out string error)
        {
            return OpenStaadSupportAdapter.TryRemoveSupportFromNode(supportObj, nodeId, out error);
        }

        private static bool TryGetSupportType(object supportObj, int nodeId, out int supportType, out string error)
        {
            return OpenStaadSupportAdapter.TryGetSupportType(supportObj, nodeId, out supportType, out error);
        }

        private static bool TryGetSupportedNodeIds(object supportObj, out int[] nodeIds, out string error)
        {
            return OpenStaadSupportAdapter.TryGetSupportedNodeIds(supportObj, out nodeIds, out error);
        }

        private static bool TryGetSupportDefinition(
            object supportObj,
            int nodeId,
            out SupportDefinition definition,
            out string error)
        {
            definition = default;
            error = string.Empty;

            if (!OpenStaadSupportAdapter.TryGetSupportInformationEx(
                    supportObj,
                    nodeId,
                    out int supportId,
                    out int supportType,
                    out int[] releaseStates,
                    out double[] springSpec,
                    out error))
            {
                return false;
            }

            definition = new SupportDefinition(
                supportId,
                supportType,
                NormalizeReturnedReleaseStates(releaseStates, springSpec),
                NormalizeSpringSpec(springSpec));

            return supportId > 0 && supportType > 0;
        }

        private static int[] NormalizeReturnedReleaseStates(int[] states, double[] springSpec)
        {
            var result = new int[6];
            for (int i = 0; i < result.Length; i++)
            {
                int state = i < states.Length ? states[i] : 0;
                double spring = i < springSpec.Length ? springSpec[i] : 0.0;

                if (Math.Abs(spring) > SpringTolerance || state < 0)
                {
                    result[i] = -1;
                }
                else
                {
                    result[i] = state != 0 ? 1 : 0;
                }
            }

            return result;
        }

        private static void AppendNodeError(StringBuilder errors, int nodeId, string message)
        {
            if (errors.Length > 0)
                errors.AppendLine();

            errors.Append("Node ")
                  .Append(nodeId)
                  .Append(": ")
                  .Append(string.IsNullOrWhiteSpace(message) ? "Unknown error." : message);
        }

        private struct SupportDefinition
        {
            public SupportDefinition(int supportId, int supportType, int[] releaseStates, double[] springSpec)
            {
                SupportId = supportId;
                SupportType = supportType;
                ReleaseStates = releaseStates ?? new int[6];
                SpringSpec = springSpec ?? new double[6];
            }

            public int SupportId { get; }
            public int SupportType { get; }
            public int[] ReleaseStates { get; }
            public double[] SpringSpec { get; }
        }
    }
}
