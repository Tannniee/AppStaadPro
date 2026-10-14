using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        internal static bool TryCaptureSectionPropertyFingerprint(
            object os,
            int propertyId,
            out SectionPropertyFingerprint fingerprint,
            out string error)
        {
            fingerprint = null;
            error = string.Empty;

            if (os == null || propertyId <= 0)
            {
                error = "Invalid model or property reference.";
                return false;
            }

            if (!TryGetSectionProperty(os, out object propertyObj, out error))
                return false;

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(
                    propertyObj,
                    propertyId,
                    out int typeCode,
                    out double[] values,
                    out error))
            {
                return false;
            }

            if (typeCode <= 0 || values == null || values.Length == 0)
            {
                error = $"GetSectionPropertyValuesEx returned incomplete data for property {propertyId}.";
                return false;
            }

            fingerprint = new SectionPropertyFingerprint
            {
                TypeCode = typeCode,
                Values = values.ToArray()
            };
            return true;
        }

        private static TargetPropertyResolution ResolveTargetProperty(
            object os,
            SectionInfo sourceInfo,
            SectionTransferContext transferContext)
        {
            if (os == null || sourceInfo == null)
                return TargetPropertyResolution.Failure("Invalid model or section data.");

            if (!TryGetSectionProperty(os, out object propertyObj, out string propertyError))
                return TargetPropertyResolution.Failure(propertyError);

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyList(
                    propertyObj,
                    out int[] propertyIds,
                    out string listError))
            {
                return TargetPropertyResolution.Failure("Could not read destination section list: " + listError);
            }

            SectionPropertyFingerprint sourceFingerprint = transferContext?.SourceFingerprint;
            string targetModelKey = StaadManager.GetModelCacheKey(os);
            bool hasTransferContext = transferContext != null;
            bool sameModel = hasTransferContext &&
                !string.IsNullOrWhiteSpace(transferContext.SourceModelKey) &&
                !string.IsNullOrWhiteSpace(targetModelKey) &&
                string.Equals(
                    transferContext.SourceModelKey,
                    targetModelKey,
                    StringComparison.OrdinalIgnoreCase);

            // A REF may be reused only for a normal same-model operation or when
            // the clipboard proves the destination is the source model. Even then,
            // validate its fingerprint/geometry before using it.
            bool mayReuseSourceRef = !hasTransferContext || sameModel;
            if (mayReuseSourceRef && sourceInfo.PropertyRefNo > 0 &&
                propertyIds.Contains(sourceInfo.PropertyRefNo) &&
                IsDestinationPropertyEquivalent(
                    os,
                    propertyObj,
                    sourceInfo.PropertyRefNo,
                    sourceInfo,
                    sourceFingerprint))
            {
                string existingName = GetDestinationPropertyName(
                    propertyObj,
                    sourceInfo.PropertyRefNo,
                    sourceInfo.Name);
                return TargetPropertyResolution.Reused(
                    sourceInfo.PropertyRefNo,
                    existingName,
                    sameModel ? "Source REF verified in the current model" : "Existing section verified");
            }

            if (sourceInfo.PropertyRefNo > 0 && !mayReuseSourceRef)
            {
                Debug.WriteLine(
                    $"Ignoring source property REF {sourceInfo.PropertyRefNo} because the destination model differs.");
            }

            int equivalentId = FindEquivalentDestinationProperty(
                os,
                propertyObj,
                propertyIds,
                sourceInfo,
                sourceFingerprint,
                sourceInfo.Name);
            if (equivalentId <= 0 &&
                (sourceFingerprint != null || CanCompareGeometryWithoutName(sourceInfo)))
            {
                equivalentId = FindEquivalentDestinationProperty(
                    os,
                    propertyObj,
                    propertyIds,
                    sourceInfo,
                    sourceFingerprint,
                    preferredName: null);
            }

            if (equivalentId > 0)
            {
                string equivalentName = GetDestinationPropertyName(propertyObj, equivalentId, sourceInfo.Name);
                return TargetPropertyResolution.Reused(
                    equivalentId,
                    equivalentName,
                    "Equivalent section reused in destination model");
            }

            SectionInfo createInfo = sourceInfo.Clone();
            if (createInfo.CreateAsUPT && HasPropertyName(propertyObj, propertyIds, createInfo.Name))
            {
                createInfo.Name = CreateUniquePropertyName(propertyObj, propertyIds, createInfo.Name);
            }

            Func<object, string, int> equivalentChecker = (model, requestedName) =>
            {
                if (!TryGetSectionProperty(model, out object candidatePropertyObj, out string _))
                    return 0;

                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyList(
                        candidatePropertyObj,
                        out int[] candidateIds,
                        out string _))
                {
                    return 0;
                }

                return FindEquivalentDestinationProperty(
                    model,
                    candidatePropertyObj,
                    candidateIds,
                    sourceInfo,
                    sourceFingerprint,
                    requestedName);
            };

            int createdId = GetOrCreateProperty(os, createInfo, equivalentChecker);
            if (createdId <= 0)
            {
                return TargetPropertyResolution.Failure(
                    $"OpenSTAAD could not create destination section '{createInfo.Name}'.");
            }

            if (!IsDestinationPropertyEquivalent(
                    os,
                    propertyObj,
                    createdId,
                    sourceInfo,
                    sourceFingerprint))
            {
                string createdName = GetDestinationPropertyName(propertyObj, createdId, createInfo.Name);
                string requestKind = hasTransferContext ? "copied section" : "requested section";
                return TargetPropertyResolution.Failure(
                    $"OpenSTAAD returned property {createdId} ('{createdName}'), but its type/values do not match the {requestKind}. Assignment was cancelled.");
            }

            string destinationName = GetDestinationPropertyName(propertyObj, createdId, createInfo.Name);
            return TargetPropertyResolution.Created(
                createdId,
                destinationName,
                "Section created and verified in destination model");
        }

        private static int FindEquivalentDestinationProperty(
            object os,
            object propertyObj,
            IEnumerable<int> propertyIds,
            SectionInfo sourceInfo,
            SectionPropertyFingerprint sourceFingerprint,
            string preferredName)
        {
            if (propertyIds == null)
                return 0;

            foreach (int propertyId in propertyIds.Where(id => id > 0).Distinct())
            {
                if (!string.IsNullOrWhiteSpace(preferredName))
                {
                    string candidateName = GetDestinationPropertyName(propertyObj, propertyId, string.Empty);
                    if (!AreSectionNamesEquivalent(candidateName, preferredName))
                        continue;
                }

                if (IsDestinationPropertyEquivalent(
                        os,
                        propertyObj,
                        propertyId,
                        sourceInfo,
                        sourceFingerprint))
                {
                    return propertyId;
                }
            }

            return 0;
        }

        private static bool IsDestinationPropertyEquivalent(
            object os,
            object propertyObj,
            int propertyId,
            SectionInfo sourceInfo,
            SectionPropertyFingerprint sourceFingerprint)
        {
            if (propertyId <= 0 || propertyObj == null || sourceInfo == null)
                return false;

            if (sourceFingerprint != null)
            {
                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(
                        propertyObj,
                        propertyId,
                        out int candidateType,
                        out double[] candidateValues,
                        out string fingerprintError))
                {
                    Debug.WriteLine(
                        $"Could not fingerprint destination property {propertyId}: {fingerprintError}");
                    return false;
                }

                var candidateFingerprint = new SectionPropertyFingerprint
                {
                    TypeCode = candidateType,
                    Values = candidateValues ?? Array.Empty<double>()
                };
                return AreFingerprintsEquivalent(sourceFingerprint, candidateFingerprint);
            }

            try
            {
                string candidateName = GetDestinationPropertyName(propertyObj, propertyId, string.Empty);
                if (!AreSectionNamesEquivalent(sourceInfo.Name, candidateName) &&
                    !CanCompareGeometryWithoutName(sourceInfo))
                    return false;

                // A direct Legacy request comes from a named STAAD section
                // catalog. Its dimensions are not editable in the UI and the
                // hidden dimension controls can retain values from a previous
                // section type. For this path the catalog name plus a Legacy
                // property type is the stable identity. Cross-model clipboard
                // operations never use this shortcut because they carry a
                // source fingerprint and are handled above.
                if (IsDirectLegacyCatalogRequest(sourceInfo))
                {
                    return OpenStaadPropertyAdapter.TryGetSectionPropertyType(
                            propertyObj,
                            propertyId,
                            out int legacyTypeCode,
                            out string legacyTypeError) &&
                        IsLegacyCatalogTypeCode(legacyTypeCode);
                }

                SectionInfo candidateInfo = BuildSectionInfoFromProperty(os, propertyId, candidateName, string.Empty);
                return AreLogicalSectionsEquivalent(sourceInfo, candidateInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not compare destination property {propertyId}: {ex.Message}");
                return false;
            }
        }

        private static bool AreFingerprintsEquivalent(
            SectionPropertyFingerprint source,
            SectionPropertyFingerprint candidate)
        {
            if (source == null || candidate == null ||
                source.TypeCode <= 0 || source.TypeCode != candidate.TypeCode)
            {
                return false;
            }

            double[] left = source.Values ?? Array.Empty<double>();
            double[] right = candidate.Values ?? Array.Empty<double>();
            int[] comparableIndices = GetComparableFingerprintIndices(source.TypeCode, left);
            if (comparableIndices.Length == 0)
                return false;

            foreach (int index in comparableIndices)
            {
                double a = index < left.Length ? left[index] : 0.0;
                double b = index < right.Length ? right[index] : 0.0;

                // ANGLE R is a required creation input. Older/source UPTs can
                // expose zero here, so compare against the same safe fallback
                // used by SectionUptModule when recreating the section.
                if (source.TypeCode == 692 && index == 4 && a <= 1e-12)
                {
                    double depth = left.Length > 1 ? left[1] : 0.0;
                    double width = left.Length > 2 ? left[2] : 0.0;
                    double thickness = left.Length > 3 ? left[3] : 0.0;
                    a = SectionUptModule.ComputeAnglePrincipalRadiusOfGyration(
                        depth,
                        width,
                        thickness);
                }
                else if (source.TypeCode == 692 && (index == 5 || index == 6) && a <= 1e-12)
                {
                    double depth = left.Length > 1 ? left[1] : 0.0;
                    double width = left.Length > 2 ? left[2] : 0.0;
                    double thickness = left.Length > 3 ? left[3] : 0.0;
                    a = Math.Max(0.5 * thickness * (depth + width - thickness), 1e-12);
                }

                if (!AreClose(a, b, 1e-10, 1e-6))
                    return false;
            }

            return true;
        }

        private static int[] GetComparableFingerprintIndices(int typeCode, double[] sourceValues)
        {
            switch (typeCode)
            {
                case 610: // BEAM ST
                case 611: // BEAM T
                case 616: // BEAM D
                case 620: // TEE ST
                case 630: // CHANNEL ST
                case 656: // CASTEL ST
                case 673: // PRISMATIC TEE
                case 675: // TAPERED TUBE
                case 699: // UPT PRISMATIC
                    return Enumerable.Range(0, 8).ToArray();
                case 612: // BEAM CM
                case 615: // BEAM TB
                case 663: // ZEES COLD ST WITH LIPS
                    return Enumerable.Range(0, 12).ToArray();
                case 613: // BEAM TC
                case 614: // BEAM BC
                case 634: // CHANNEL COLD ST
                case 644: // ANGLE COLD ST
                case 662: // ZEES COLD ST
                case 676: // PRISMATIC GENERAL
                    return Enumerable.Range(0, 10).ToArray();
                case 631: // CHANNEL D
                case 633: // CHANNEL FR
                    return Enumerable.Range(0, 9).ToArray();
                case 640: // ANGLE ST
                case 641: // ANGLE RA
                case 646: // ANGLE SA
                case 650: // TUBE ST
                case 654: // HSS RECTANGLE
                case 668: // SOLID ROUND
                case 674: // PRISMATIC TRAP
                    return Enumerable.Range(0, 7).ToArray();
                case 642: // ANGLE LD
                case 643: // ANGLE SD
                    return Enumerable.Range(0, 8).ToArray();
                case 645: // ANGLE COLD ST WITH LIPS
                case 635: // CHANNEL COLD ST WITH LIPS
                case 664: // HAT COLD ST
                    return Enumerable.Range(0, 11).ToArray();
                case 655: // HSS ROUND
                case 660: // PIPE ST
                case 666: // PLATE STRIP
                case 672: // PRISMATIC RECTANGLE
                    return Enumerable.Range(0, 6).ToArray();
                case 671: // PRISMATIC CIRCLE
                    return Enumerable.Range(0, 5).ToArray();
                case 680: // TAPERED I: F1..F7 only.
                    return Enumerable.Range(0, 7).ToArray();
                case 690: // WIDE FLANGE: WF1/TF1 are only explicit for unequal flanges.
                    bool hasUnequalFlangeValues = sourceValues != null && sourceValues.Length > 11 &&
                        (sourceValues[10] > 0.0 || sourceValues[11] > 0.0);
                    return Enumerable.Range(0, hasUnequalFlangeValues ? 12 : 10).ToArray();
                case 691: // CHANNEL
                    return Enumerable.Range(0, 11).ToArray();
                case 692: // ANGLE: AX/IZ/IY/IX are derived by OpenSTAAD.
                    return Enumerable.Range(1, 6).ToArray();
                case 693: // DOUBLE ANGLE: AX is derived by OpenSTAAD.
                    return Enumerable.Range(1, 10).ToArray();
                case 694: // TEE
                    return Enumerable.Range(0, 11).ToArray();
                case 695: // PIPE: creation API accepts OD/ID/AY/AZ only.
                    return Enumerable.Range(1, 4).ToArray();
                case 696: // TUBE
                    return Enumerable.Range(0, 9).ToArray();
                case 697: // GENERAL
                    return Enumerable.Range(0, 16).ToArray();
                case 698: // ISECTION
                    return Enumerable.Range(0, 10).ToArray();
                default:
                    int count = sourceValues?.Length ?? 0;
                    return count > 0 ? Enumerable.Range(0, count).ToArray() : Array.Empty<int>();
            }
        }

        private static bool CanCompareGeometryWithoutName(SectionInfo sourceInfo)
        {
            // OpenSTAAD generates its own display names for properties created
            // through the direct geometry APIs. In these cases the generated
            // name must not decide equivalence; the type and editable geometry
            // are still checked by AreLogicalSectionsEquivalent.
            return sourceInfo != null &&
                !sourceInfo.CreateAsUPT &&
                (sourceInfo.PropType == eFramePropType.I ||
                 sourceInfo.PropType == eFramePropType.Circle ||
                 sourceInfo.PropType == eFramePropType.Rectangular ||
                 sourceInfo.PropType == eFramePropType.T ||
                 sourceInfo.PropType == eFramePropType.Trapezoidal ||
                 sourceInfo.PropType == eFramePropType.Pipe);
        }

        private static bool IsDirectLegacyCatalogRequest(SectionInfo sourceInfo)
        {
            return sourceInfo != null &&
                !sourceInfo.CreateAsUPT &&
                sourceInfo.PropType == eFramePropType.Legacy &&
                !string.IsNullOrWhiteSpace(sourceInfo.Name);
        }

        private static bool IsLegacyCatalogTypeCode(int typeCode)
        {
            return (typeCode >= 610 && typeCode <= 669) || typeCode == 61100;
        }

        private static bool AreLogicalSectionsEquivalent(SectionInfo source, SectionInfo candidate)
        {
            if (source == null || candidate == null ||
                source.PropType != candidate.PropType ||
                source.CreateAsUPT != candidate.CreateAsUPT)
                return false;

            if (!AreLogicalGeometryValuesEquivalent(source, candidate))
                return false;

            if (!source.CreateAsUPT && !candidate.CreateAsUPT)
                return true;

            double sourceUptAy = source.UptAy;
            double sourceUptAz = source.UptAz;
            if (source.PropType == eFramePropType.Angle)
            {
                double angleArea = (source.Tf / 1000.0) *
                    ((source.H1 + source.Bf - source.Tf) / 1000.0);
                if (sourceUptAy <= 1e-12)
                    sourceUptAy = Math.Max(0.5 * angleArea, 1e-12);
                if (sourceUptAz <= 1e-12)
                    sourceUptAz = Math.Max(0.5 * angleArea, 1e-12);
            }

            double[] sourceUpt =
            {
                source.UptAx, source.UptIz, source.UptIy, source.UptIx,
                sourceUptAy, sourceUptAz, source.UptSz, source.UptSy,
                source.UptPz, source.UptPy, source.UptHss, source.UptDee,
                source.PropType == eFramePropType.Angle && source.UptR <= 1e-12
                    ? SectionUptModule.ComputeAnglePrincipalRadiusOfGyration(
                        source.H1 / 1000.0,
                        source.Bf / 1000.0,
                        source.Tf / 1000.0)
                    : source.UptR,
                source.UptCz, source.UptCy, source.UptXif
            };
            double[] candidateUpt =
            {
                candidate.UptAx, candidate.UptIz, candidate.UptIy, candidate.UptIx,
                candidate.UptAy, candidate.UptAz, candidate.UptSz, candidate.UptSy,
                candidate.UptPz, candidate.UptPy, candidate.UptHss, candidate.UptDee,
                candidate.UptR, candidate.UptCz, candidate.UptCy, candidate.UptXif
            };

            for (int i = 0; i < sourceUpt.Length; i++)
            {
                // Zero means that the UI did not explicitly supply the
                // mechanical value. SectionUptModule then computes a positive
                // fallback before creation, so zero must not be compared with
                // that derived result. Values captured from an existing UPT
                // are positive and remain strictly checked.
                if (Math.Abs(sourceUpt[i]) <= 1e-12)
                    continue;

                if (!AreClose(sourceUpt[i], candidateUpt[i], 1e-10, 1e-5))
                    return false;
            }

            return true;
        }

        private static bool AreLogicalGeometryValuesEquivalent(SectionInfo source, SectionInfo candidate)
        {
            switch (source.PropType)
            {
                case eFramePropType.I:
                    if (source.CreateAsUPT)
                    {
                        return AreGeometryFieldsClose(source, candidate, 0, 2, 3, 4) &&
                            AreOptionalGeometryFieldClose(source.Bfb, candidate.Bfb) &&
                            AreOptionalGeometryFieldClose(source.Tfb, candidate.Tfb);
                    }

                    return AreGeometryFieldsClose(source, candidate, 0, 1, 2, 3, 4, 5, 6);

                case eFramePropType.Rectangular:
                case eFramePropType.Box:
                    if (!AreClose(source.H1, candidate.H1, 0.02, 1e-5))
                        return false;

                    double sourceWidth = source.CreateAsUPT
                        ? PositiveOrFallback(source.Bf, source.H2)
                        : source.H2;
                    double candidateWidth = candidate.CreateAsUPT
                        ? PositiveOrFallback(candidate.Bf, candidate.H2)
                        : candidate.H2;
                    if (!AreClose(sourceWidth, candidateWidth, 0.02, 1e-5))
                        return false;

                    return !source.CreateAsUPT ||
                        AreClose(source.Tf, candidate.Tf, 0.02, 1e-5);

                case eFramePropType.Pipe:
                    if (!AreClose(source.H1, candidate.H1, 0.02, 1e-5))
                        return false;
                    if (source.CreateAsUPT)
                        return AreClose(source.Bf, candidate.Bf, 0.02, 1e-5);

                    return AreClose(source.H2, candidate.H2, 0.02, 1e-5) &&
                        AreClose(source.Tw, candidate.Tw, 0.02, 1e-5);

                case eFramePropType.Circle:
                    return AreClose(source.H1, candidate.H1, 0.02, 1e-5);

                case eFramePropType.Trapezoidal:
                    return AreGeometryFieldsClose(source, candidate, 0, 1, 3);

                case eFramePropType.T:
                case eFramePropType.Channel:
                    return AreGeometryFieldsClose(source, candidate, 0, 2, 3, 4);

                case eFramePropType.Angle:
                    return AreGeometryFieldsClose(source, candidate, 0, 3, 4);

                case eFramePropType.DblAngle:
                    return AreGeometryFieldsClose(source, candidate, 0, 3, 4, 7);

                case eFramePropType.DbChannel:
                    return AreGeometryFieldsClose(source, candidate, 0, 2, 3, 4, 7);

                case eFramePropType.General:
                    return AreGeometryFieldsClose(source, candidate, 0, 1, 2, 4);

                case eFramePropType.ISection:
                    return AreGeometryFieldsClose(source, candidate, 0, 1, 2, 3, 4, 5, 6);

                case eFramePropType.Prismatic:
                    return AreGeometryFieldsClose(source, candidate, 0, 1);

                case eFramePropType.Legacy:
                    // Direct Legacy catalog requests are resolved by catalog
                    // identity before this method. Fingerprinted transfers are
                    // handled by AreFingerprintsEquivalent.
                    return true;

                default:
                    return AreGeometryFieldsClose(source, candidate, 0, 1, 2, 3, 4, 5, 6, 7);
            }
        }

        private static bool AreGeometryFieldsClose(
            SectionInfo source,
            SectionInfo candidate,
            params int[] fieldIndices)
        {
            double[] sourceValues = GetGeometryValues(source);
            double[] candidateValues = GetGeometryValues(candidate);
            foreach (int index in fieldIndices ?? Array.Empty<int>())
            {
                if (index < 0 || index >= sourceValues.Length ||
                    !AreClose(sourceValues[index], candidateValues[index], 0.02, 1e-5))
                {
                    return false;
                }
            }

            return true;
        }

        private static double[] GetGeometryValues(SectionInfo info)
        {
            return new[]
            {
                info.H1, info.H2, info.Tw, info.Bf, info.Tf,
                info.Bfb, info.Tfb, info.Dis
            };
        }

        private static bool AreOptionalGeometryFieldClose(double source, double candidate)
        {
            return source <= 0.0 || AreClose(source, candidate, 0.02, 1e-5);
        }

        private static double PositiveOrFallback(double primary, double fallback)
        {
            return primary > 0.0 ? primary : fallback;
        }

        private static bool AreClose(double left, double right, double absoluteTolerance, double relativeTolerance)
        {
            if (double.IsNaN(left) || double.IsNaN(right) ||
                double.IsInfinity(left) || double.IsInfinity(right))
            {
                return false;
            }

            double difference = Math.Abs(left - right);
            double scale = Math.Max(Math.Abs(left), Math.Abs(right));
            return difference <= absoluteTolerance + (relativeTolerance * scale);
        }

        private static bool HasPropertyName(object propertyObj, IEnumerable<int> propertyIds, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || propertyIds == null)
                return false;

            foreach (int propertyId in propertyIds)
            {
                string candidateName = GetDestinationPropertyName(propertyObj, propertyId, string.Empty);
                if (AreSectionNamesEquivalent(candidateName, name))
                    return true;
            }

            return false;
        }

        private static string CreateUniquePropertyName(
            object propertyObj,
            IEnumerable<int> propertyIds,
            string sourceName)
        {
            string baseName = string.IsNullOrWhiteSpace(sourceName) ? "COPIED_SECTION" : sourceName.Trim();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (int propertyId in propertyIds ?? Array.Empty<int>())
            {
                string name = GetDestinationPropertyName(propertyObj, propertyId, string.Empty);
                if (!string.IsNullOrWhiteSpace(name))
                    usedNames.Add(name.Trim());
            }

            string candidate = baseName + "_COPY";
            if (!usedNames.Contains(candidate))
                return candidate;

            for (int suffix = 2; suffix <= 999; suffix++)
            {
                candidate = baseName + "_COPY" + suffix;
                if (!usedNames.Contains(candidate))
                    return candidate;
            }

            throw new SectionPropertyException(
                $"Could not create a unique destination name for UPT section '{baseName}'.");
        }

        private static string GetDestinationPropertyName(object propertyObj, int propertyId, string fallback)
        {
            if (OpenStaadPropertyAdapter.TryGetSectionPropertyName(
                    propertyObj,
                    propertyId,
                    out string name,
                    out string _) &&
                !string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            return fallback ?? string.Empty;
        }

        private sealed class TargetPropertyResolution
        {
            public int PropertyId { get; private set; }
            public string PropertyName { get; private set; }
            public string Description { get; private set; }
            public string ErrorMessage { get; private set; }

            public static TargetPropertyResolution Reused(int id, string name, string description)
            {
                return Create(id, name, description, string.Empty);
            }

            public static TargetPropertyResolution Created(int id, string name, string description)
            {
                return Create(id, name, description, string.Empty);
            }

            public static TargetPropertyResolution Failure(string error)
            {
                return Create(0, string.Empty, string.Empty, error);
            }

            private static TargetPropertyResolution Create(
                int id,
                string name,
                string description,
                string error)
            {
                return new TargetPropertyResolution
                {
                    PropertyId = id,
                    PropertyName = name ?? string.Empty,
                    Description = description ?? string.Empty,
                    ErrorMessage = error ?? string.Empty
                };
            }
        }
    }
}
