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
    public static partial class SupportService
    {
        private const int SupportTypePinned = 1;
        private const int SupportTypeFixed = 2;
        private const int SupportTypeFixedBut = 3;
        private const int SupportTypeGeneratedPinned = 11;
        private const int SupportTypeGeneratedFixed = 12;
        private const int SupportTypeGeneratedFixedBut = 13;
        private const double SpringTolerance = 1e-9;

        public static ServiceOperationResult ApplyFixed_Selected()
        {
            return ApplySimpleSupportToSelectedNodes(
                supportType: SupportTypeFixed,
                title: "Support");
        }

        public static ServiceOperationResult ApplyPinned_Selected()
        {
            return ApplySimpleSupportToSelectedNodes(
                supportType: SupportTypePinned,
                title: "Support");
        }

        public static ServiceOperationResult RemoveSupport_Selected()
        {
            if (!MiscOpenStaadService.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionMessage))
                return ServiceOperationResult.Error("Support", connectionMessage);

            object openStaad = connection.OpenStaad;
            if (!MiscSelectionService.TryGetSelectedNodeIds(openStaad, out int[] nodeIds, out string selectionMessage))
                return ServiceOperationResult.Info("Support", selectionMessage);

            if (!TryGetSupportObject(openStaad, out object supportObj, out string supportError))
                return ServiceOperationResult.Error("Support", supportError);

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

            return ShowNodeOperationResult(
                actionName: "Support",
                successMessage: $"Support removed from {removedCount}/{nodeIds.Length} node(s).",
                noOpMessage: "Selected nodes do not have any support assigned.",
                removedOrAssignedCount: removedCount,
                totalCount: nodeIds.Length,
                errors: errors);
        }

        public static ServiceOperationResult ApplyFixedBut_Selected(int[] releaseSpec, double[] springSpec)
        {
            if (!MiscOpenStaadService.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionMessage))
                return ServiceOperationResult.Error("Fixed But", connectionMessage);

            object openStaad = connection.OpenStaad;
            if (!MiscSelectionService.TryGetSelectedNodeIds(openStaad, out int[] nodeIds, out string selectionMessage))
            {
                return ServiceOperationResult.Info("Support", selectionMessage);
            }

            int[] normalizedRelease = NormalizeReleaseSpec(releaseSpec);
            double[] normalizedSpring = NormalizeSpringSpec(springSpec);
            int[] desiredStates = BuildDesiredSupportStates(normalizedRelease, normalizedSpring);

            if (!HasAnyFixedButDefinition(desiredStates, normalizedSpring))
            {
                return ServiceOperationResult.Warning(
                    "Fixed But",
                    "Please define at least one release or spring stiffness.");
            }

            if (!TryGetSupportObject(openStaad, out object supportObj, out string supportError))
                return ServiceOperationResult.Error("Fixed But", supportError);

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
                    return ServiceOperationResult.Error("Fixed But", message);
                }
            }

            return AssignSupportAndReport(
                supportObj,
                nodeIds,
                supportId,
                successTitle: "Fixed But",
                successVerb: "Fixed But applied",
                reusedExisting: reusedExisting);
        }

        private static ServiceOperationResult ApplySimpleSupportToSelectedNodes(int supportType, string title)
        {
            if (!MiscOpenStaadService.TryGetConnection(out StaadConnectionSnapshot connection, out string connectionMessage))
                return ServiceOperationResult.Error(title, connectionMessage);

            object openStaad = connection.OpenStaad;
            if (!MiscSelectionService.TryGetSelectedNodeIds(openStaad, out int[] nodeIds, out string selectionMessage))
                return ServiceOperationResult.Info(title, selectionMessage);

            if (!TryGetSupportObject(openStaad, out object supportObj, out string supportError))
                return ServiceOperationResult.Error(title, supportError);

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
                    return ServiceOperationResult.Error(title, message);
                }
            }

            return AssignSupportAndReport(
                supportObj,
                nodeIds,
                supportId,
                successTitle: title,
                successVerb: supportType == SupportTypeFixed ? "Fixed applied" : "Pinned applied",
                reusedExisting: reusedExisting);
        }

        private static ServiceOperationResult AssignSupportAndReport(
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
            return ShowNodeOperationResult(
                actionName: successTitle,
                successMessage: successMessage,
                noOpMessage: "No node was updated.",
                removedOrAssignedCount: assignedCount,
                totalCount: nodeIds.Length,
                errors: errors);
        }

        private static ServiceOperationResult ShowNodeOperationResult(
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
                return ServiceOperationResult.Success(actionName, successMessage);
            }

            if (removedOrAssignedCount <= 0 && string.IsNullOrEmpty(errorText))
            {
                return ServiceOperationResult.Info(actionName, noOpMessage);
            }

            var message = new StringBuilder();
            if (removedOrAssignedCount > 0)
                message.AppendLine(successMessage);
            if (!string.IsNullOrEmpty(errorText))
                message.Append(errorText);

            return ServiceOperationResult.Warning(actionName, message.ToString().Trim());
        }

        private static bool TryGetSupportObject(object openStaad, out object supportObj, out string error)
        {
            supportObj = null;
            if (OpenStaadSupportAdapter.TryGetSupportObject(openStaad, out supportObj, out error))
                return true;

            return false;
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
