from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import pythoncom
import win32com.client

from .models import ModelGeometry, ModelResultField
from .std_parser import parse_std_geometry


class StaadConnectionError(RuntimeError):
    """Raised when the app cannot connect to the active STAAD session."""


@dataclass(slots=True)
class StaadSessionInfo:
    model_path: Path
    node_count_hint: int | None


@dataclass(slots=True)
class ResultMetadata:
    results_available: bool
    displacement_unit: str | None
    force_unit: str | None = None


class StaadConnector:
    _DISPID_GET_OUTPUT_UNIT_FOR_DISPLACEMENT = 7
    _DISPID_GET_NODE_DISPLACEMENTS = 14
    _DISPID_GET_MAX_SECTION_DISPLACEMENT = 34
    _DISPID_ARE_RESULTS_AVAILABLE = 38
    _DISPID_GET_BEAM_PROPERTY = 41
    _DISPID_GET_BETA_ANGLE = 42
    _MODEL_LENGTH_TO_MM = {
        "MM": 1.0,
        "MMS": 1.0,
        "MILLIMETER": 1.0,
        "MILLIMETRE": 1.0,
        "CM": 10.0,
        "CENTIMETER": 10.0,
        "CENTIMETRE": 10.0,
        "M": 1000.0,
        "METER": 1000.0,
        "METRE": 1000.0,
        "IN": 25.4,
        "INCH": 25.4,
        "FT": 304.8,
        "FEET": 304.8,
        "FOOT": 304.8,
    }

    def __init__(self) -> None:
        self._openstaad = None

    def connect(self) -> StaadSessionInfo:
        pythoncom.CoInitialize()

        try:
            self._openstaad = win32com.client.GetActiveObject("StaadPro.OpenSTAAD")
        except Exception as exc:  # pragma: no cover - live COM only
            raise StaadConnectionError(
                "Could not attach to OpenSTAAD. Make sure STAAD.Pro is open with a model loaded."
            ) from exc

        model_path = self._get_active_model_path()
        node_count_hint = self._get_node_count_hint()
        return StaadSessionInfo(model_path=model_path, node_count_hint=node_count_hint)

    def load_geometry(self, load_section_metadata: bool = False) -> ModelGeometry:
        session = self.connect()
        if not session.model_path.exists():
            raise StaadConnectionError(f"Active STAAD file was not found: {session.model_path}")

        model = parse_std_geometry(session.model_path)
        if load_section_metadata:
            self.load_section_metadata(model)

        return model

    def load_section_metadata(self, model: ModelGeometry) -> ModelGeometry:
        if model.section_metadata_loaded:
            return model

        self.connect()
        beam_property_scale = self._beam_property_length_to_model_factor(model.unit_length)

        property_obj = getattr(self._openstaad, "Property", None)
        if property_obj is not None:
            for beam in model.beams:
                self._load_beam_section_metadata(property_obj, beam, beam_property_scale)

        model.section_metadata_loaded = True
        return model

    def _load_beam_section_metadata(self, property_obj, beam, beam_property_scale: float) -> None:
        beam.beta_angle = self._get_beta_angle(property_obj, beam.beam_id)
        section_name = self._get_beam_section_name(property_obj, beam.beam_id)
        section_display_name = self._get_beam_section_display_name(property_obj, beam.beam_id)
        section_property_type = self._get_beam_section_property_type(property_obj, beam.beam_id)
        section_property_values = self._get_beam_section_property_values(
            property_obj,
            beam.beam_id,
            section_property_type,
            beam_property_scale,
        )
        width, depth, flange_thickness_top, web_thickness = self._get_beam_long_properties(
            property_obj,
            beam.beam_id,
            beam_property_scale,
        )
        beam.width = None if width is None else width * beam_property_scale
        beam.depth = None if depth is None else depth * beam_property_scale
        beam.web_thickness = web_thickness
        beam.flange_thickness_top = flange_thickness_top
        beam.flange_thickness_bottom = flange_thickness_top
        beam.section_name = section_name
        beam.section_display_name = section_display_name
        beam.section_property_type = section_property_type
        beam.section_property_values = section_property_values

    def get_result_metadata(self) -> ResultMetadata:
        self.connect()
        return ResultMetadata(
            results_available=self._are_results_available(),
            displacement_unit=self._get_output_displacement_unit(),
            force_unit=self._get_output_force_unit(),
        )

    def load_global_displacement_field(
        self,
        model: ModelGeometry,
        case_id: int,
        component: str,
        include_beam_values: bool = True,
        beam_ids: set[int] | None = None,
    ) -> ModelResultField:
        self.connect()

        component = component.upper()
        if component not in {"DX", "DY", "DZ"}:
            raise ValueError(f"Unsupported displacement component: {component}")

        if not self._are_results_available():
            raise StaadConnectionError("Analysis results are not available for the active STAAD model.")

        scale_to_mm = self._model_length_to_mm_factor(model.unit_length)
        display_to_model_scale = 1.0 / scale_to_mm
        field = ModelResultField(
            case_id=case_id,
            component=component,
            unit="mm",
            display_to_model_scale=display_to_model_scale,
        )
        index = {"DX": 0, "DY": 1, "DZ": 2}[component]

        for node_id in model.nodes:
            displacements = self._get_node_displacements(node_id, case_id)
            field.node_values[node_id] = displacements[index] * scale_to_mm

        if include_beam_values:
            selected_beam_ids = set(beam_ids) if beam_ids else None
            for beam in model.beams:
                if selected_beam_ids is not None and beam.beam_id not in selected_beam_ids:
                    continue
                value, position = self._get_beam_peak_global_displacement(beam.beam_id, index, case_id, model)
                field.beam_values[beam.beam_id] = value
                field.beam_peak_positions[beam.beam_id] = position

        return field

    def load_support_reaction_values(
        self,
        case_id: int,
        node_ids: list[int],
        component: str,
    ) -> tuple[dict[int, float], str | None]:
        self.connect()

        component = component.upper()
        if component not in {"FX", "FY", "FZ", "MX", "MY", "MZ"}:
            raise ValueError(f"Unsupported support reaction component: {component}")

        if not self._are_results_available():
            raise StaadConnectionError("Analysis results are not available for the active STAAD model.")

        index = {"FX": 0, "FY": 1, "FZ": 2, "MX": 3, "MY": 4, "MZ": 5}[component]
        values: dict[int, float] = {}
        for node_id in node_ids:
            reactions = self._get_support_reactions(node_id, case_id)
            values[node_id] = reactions[index]
        return values, self._get_output_force_unit()

    def _get_active_model_path(self) -> Path:
        variant = win32com.client.VARIANT(pythoncom.VT_BYREF | pythoncom.VT_BSTR, "")
        self._openstaad.GetSTAADFile(variant, True)
        path = Path(variant.value)
        if not str(path).strip():
            raise StaadConnectionError("OpenSTAAD did not return an active STAAD file path.")
        return path

    def _get_node_count_hint(self) -> int | None:
        try:
            return int(self._openstaad.Geometry.GetNodeCount)
        except Exception:
            return None

    def _are_results_available(self) -> bool:
        try:
            value = self._openstaad.Output._oleobj_.Invoke(
                self._DISPID_ARE_RESULTS_AVAILABLE,
                0,
                pythoncom.DISPATCH_METHOD,
                True,
            )
            return value != 0
        except Exception:
            return False

    def _get_beta_angle(self, property_obj, beam_id: int) -> float:
        try:
            value = property_obj._oleobj_.Invoke(
                self._DISPID_GET_BETA_ANGLE,
                0,
                pythoncom.DISPATCH_METHOD,
                True,
                beam_id,
            )
            return float(value)
        except Exception:
            return 0.0

    def _get_beam_long_properties(
        self,
        property_obj,
        beam_id: int,
        beam_property_scale: float,
    ) -> tuple[float | None, float | None, float | None, float | None]:
        try:
            dispid = property_obj._oleobj_.GetIDsOfNames("GetBeamPropertyAll")
            result = property_obj._oleobj_.InvokeTypes(
                dispid,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_EMPTY, 0),
                (
                    (pythoncom.VT_I4, 1),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                ),
                beam_id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
            )
            if isinstance(result, tuple) and len(result) >= 10:
                width = float(result[0])
                depth = float(result[1])
                flange_thickness = float(result[8])
                web_thickness = float(result[9])
                return (
                    width,
                    depth,
                    flange_thickness * beam_property_scale,
                    web_thickness * beam_property_scale,
                )
        except Exception:
            pass
        width, depth = self._get_beam_dimensions(property_obj, beam_id)
        return width, depth, None, None

    def _get_beam_dimensions(self, property_obj, beam_id: int) -> tuple[float | None, float | None]:
        try:
            result = property_obj._oleobj_.InvokeTypes(
                self._DISPID_GET_BEAM_PROPERTY,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_EMPTY, 0),
                (
                    (pythoncom.VT_I4, 1),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                ),
                beam_id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0
            )
            if isinstance(result, tuple) and len(result) >= 2:
                return float(result[0]), float(result[1])
        except Exception:
            pass
        return None, None

    def _get_beam_section_name(self, property_obj, beam_id: int) -> str | None:
        try:
            dispid = property_obj._oleobj_.GetIDsOfNames("GetBeamSectionName")
            value = property_obj._oleobj_.Invoke(dispid, 0, pythoncom.DISPATCH_METHOD, True, beam_id)
            text = str(value).strip()
            return text or None
        except Exception:
            return None

    def _get_beam_section_display_name(self, property_obj, beam_id: int) -> str | None:
        try:
            dispid = property_obj._oleobj_.GetIDsOfNames("GetBeamSectionDisplayName")
            value = property_obj._oleobj_.Invoke(dispid, 0, pythoncom.DISPATCH_METHOD, True, beam_id)
            text = str(value).strip()
            return text or None
        except Exception:
            return None

    def _get_beam_section_property_type(self, property_obj, beam_id: int) -> int | None:
        try:
            dispid = property_obj._oleobj_.GetIDsOfNames("GetBeamSectionPropertyTypeNo")
            value = property_obj._oleobj_.Invoke(dispid, 0, pythoncom.DISPATCH_METHOD, True, beam_id)
            return int(value)
        except Exception:
            return None

    def _get_beam_section_property_values(
        self,
        property_obj,
        beam_id: int,
        section_property_type: int | None,
        beam_property_scale: float,
    ) -> tuple[float, ...]:
        try:
            dispid = property_obj._oleobj_.GetIDsOfNames("GetBeamSectionPropertyValuesEx")
            result = property_obj._oleobj_.InvokeTypes(
                dispid,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_I4, 0),
                (
                    (pythoncom.VT_I4, 1),
                    (pythoncom.VT_BYREF | pythoncom.VT_I4, 2),
                    (pythoncom.VT_BYREF | pythoncom.VT_ARRAY | pythoncom.VT_R8, 2),
                ),
                beam_id,
                0,
                [0.0] * 24,
            )
            if not isinstance(result, tuple) or len(result) < 3:
                return ()
            raw_values = tuple(float(item) for item in result[2])
            return self._normalize_section_property_values(
                section_property_type,
                raw_values,
                beam_property_scale,
            )
        except Exception:
            return ()

    def _normalize_section_property_values(
        self,
        section_property_type: int | None,
        values: tuple[float, ...],
        beam_property_scale: float,
    ) -> tuple[float, ...]:
        if section_property_type is None or not values:
            return values

        normalized = list(values)
        for index in self._section_length_value_indexes(section_property_type):
            if 0 <= index < len(normalized):
                normalized[index] = normalized[index] * beam_property_scale
        return tuple(normalized)

    def _section_length_value_indexes(self, section_property_type: int) -> tuple[int, ...]:
        return {
            668: (4, 5, 8),
            671: (4,),
            672: (4, 5),
            673: (4, 5, 6, 7),
            674: (4, 5, 6),
            675: (4, 5, 6),
            676: (6, 7, 8, 9),
            680: (0, 1, 2, 3, 4, 5, 6),
            690: (1, 2, 3, 4, 10, 11),
            691: (1, 2, 3, 4, 8),
            692: (1, 2, 3, 4),
            693: (1, 2, 3, 4, 8),
            694: (1, 2, 3, 4, 8),
            695: (1, 2),
            696: (1, 2, 3),
            697: (1, 2, 3, 4, 11, 12, 13),
            698: (0, 1, 2, 3, 4, 5, 6, 9),
            699: (6, 7),
        }.get(section_property_type, ())

    def _get_output_displacement_unit(self) -> str | None:
        direct_result = self._try_output_call("GetOutputUnitForDisplacement", "")
        direct_value = self._extract_string_result(direct_result)
        if direct_value:
            return direct_value
        try:
            result = self._openstaad.Output._oleobj_.InvokeTypes(
                self._DISPID_GET_OUTPUT_UNIT_FOR_DISPLACEMENT,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_I4, 0),
                ((pythoncom.VT_BYREF | pythoncom.VT_BSTR, 2),),
                "",
            )
            value = str(result[1]).strip()
            return value or None
        except Exception:
            return None

    def _get_output_force_unit(self) -> str | None:
        direct_result = self._try_output_call("GetOutputUnitForForce", "")
        direct_value = self._extract_string_result(direct_result)
        if direct_value:
            return direct_value
        try:
            dispid = self._openstaad.Output._oleobj_.GetIDsOfNames("GetOutputUnitForForce")
            result = self._openstaad.Output._oleobj_.InvokeTypes(
                dispid,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_I4, 0),
                ((pythoncom.VT_BYREF | pythoncom.VT_BSTR, 2),),
                "",
            )
            value = str(result[1]).strip()
            return value or None
        except Exception:
            return None

    def _get_node_displacements(self, node_id: int, case_id: int) -> tuple[float, float, float, float, float, float]:
        direct_result = self._try_output_call("GetNodeDisplacements", node_id, case_id, [0.0] * 6)
        direct_values = self._extract_displacement_array_result(direct_result, minimum_length=6)
        if direct_values is not None:
            return direct_values[:6]

        result = self._openstaad.Output._oleobj_.InvokeTypes(
            self._DISPID_GET_NODE_DISPLACEMENTS,
            0,
            pythoncom.DISPATCH_METHOD,
            (pythoncom.VT_BOOL, 0),
            (
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_BYREF | pythoncom.VT_ARRAY | pythoncom.VT_R8, 2),
            ),
            node_id,
            case_id,
            [0.0] * 6,
        )
        success, values = result
        if not success:
            raise StaadConnectionError(f"Failed to read node displacements for node {node_id} and load case {case_id}.")
        return tuple(float(value) for value in values)

    def _get_support_reactions(self, node_id: int, case_id: int) -> tuple[float, float, float, float, float, float]:
        direct_result = self._try_output_call("GetSupportReactions", node_id, case_id, [0.0] * 6)
        direct_values = None
        if not (
            isinstance(direct_result, tuple)
            and direct_result
            and isinstance(direct_result[0], bool)
            and not direct_result[0]
        ):
            direct_values = self._extract_displacement_array_result(direct_result, minimum_length=6)
        if direct_values is not None:
            return direct_values[:6]

        try:
            dispid = self._openstaad.Output._oleobj_.GetIDsOfNames("GetSupportReactions")
            result = self._openstaad.Output._oleobj_.InvokeTypes(
                dispid,
                0,
                pythoncom.DISPATCH_METHOD,
                (pythoncom.VT_BOOL, 0),
                (
                    (pythoncom.VT_I4, 1),
                    (pythoncom.VT_I4, 1),
                    (pythoncom.VT_BYREF | pythoncom.VT_ARRAY | pythoncom.VT_R8, 2),
                ),
                node_id,
                case_id,
                [0.0] * 6,
            )
        except Exception as exc:
            raise StaadConnectionError(
                f"Failed to read support reactions for node {node_id} and load case {case_id}."
            ) from exc
        success, values = result
        if not success:
            raise StaadConnectionError(
                f"Failed to read support reactions for node {node_id} and load case {case_id}."
            )
        return tuple(float(value) for value in values)

    def _get_beam_max_section_displacement(self, beam_id: int, direction: int, case_id: int) -> tuple[float, float]:
        direct_result = self._try_output_call("GetMaxSectionDisplacement", beam_id, direction, case_id, 0.0, 0.0)
        direct_values = self._extract_beam_peak_result(direct_result)
        if direct_values is not None:
            return direct_values

        result = self._openstaad.Output._oleobj_.InvokeTypes(
            self._DISPID_GET_MAX_SECTION_DISPLACEMENT,
            0,
            pythoncom.DISPATCH_METHOD,
            (pythoncom.VT_BOOL, 0),
            (
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
                (pythoncom.VT_BYREF | pythoncom.VT_R8, 2),
            ),
            beam_id,
            direction,
            case_id,
            0.0,
            0.0,
        )
        success, value, position = result
        if not success:
            raise StaadConnectionError(
                f"Failed to read beam deflection for member {beam_id} and load case {case_id}."
            )
        return float(value), float(position)

    def _get_beam_peak_global_displacement(
        self,
        beam_id: int,
        component_index: int,
        case_id: int,
        model: ModelGeometry,
    ) -> tuple[float, float]:
        beam = next((item for item in model.beams if item.beam_id == beam_id), None)
        if beam is None:
            raise StaadConnectionError(f"Beam {beam_id} was not found in the parsed model geometry.")

        start_displacements = self._get_member_end_displacements(beam_id, 0, case_id)
        end_displacements = self._get_member_end_displacements(beam_id, 1, case_id)
        start_value = start_displacements[component_index]
        end_value = end_displacements[component_index]

        start_node = model.nodes.get(beam.start_node_id)
        end_node = model.nodes.get(beam.end_node_id)
        if start_node is None or end_node is None:
            raise StaadConnectionError(f"Beam {beam_id} has missing node coordinates in the parsed model geometry.")

        dx = end_node.x - start_node.x
        dy = end_node.y - start_node.y
        dz = end_node.z - start_node.z
        length = (dx * dx + dy * dy + dz * dz) ** 0.5

        if abs(end_value) >= abs(start_value):
            return end_value * self._model_length_to_mm_factor(model.unit_length), length
        return start_value * self._model_length_to_mm_factor(model.unit_length), 0.0

    def _get_member_end_displacements(self, beam_id: int, end_index: int, case_id: int) -> tuple[float, float, float]:
        result = self._openstaad.Output._oleobj_.InvokeTypes(
            16,
            0,
            pythoncom.DISPATCH_METHOD,
            (pythoncom.VT_BOOL, 0),
            (
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_I4, 1),
                (pythoncom.VT_BYREF | pythoncom.VT_ARRAY | pythoncom.VT_R8, 2),
            ),
            beam_id,
            end_index,
            case_id,
            [0.0] * 3,
        )
        success, values = result
        if not success:
            raise StaadConnectionError(
                f"Failed to read member-end displacements for member {beam_id}, end {end_index}, and load case {case_id}."
            )
        return tuple(float(value) for value in values[:3])

    def _try_output_call(self, method_name: str, *args):
        try:
            method = getattr(self._openstaad.Output, method_name)
        except Exception:
            return None

        try:
            return method(*args)
        except Exception:
            return None

    def _extract_string_result(self, result) -> str | None:
        if result is None:
            return None
        if isinstance(result, str):
            value = result.strip()
            return value or None
        if isinstance(result, tuple):
            for item in reversed(result):
                if isinstance(item, str):
                    value = item.strip()
                    if value:
                        return value
        return None

    def _extract_displacement_array_result(
        self,
        result,
        minimum_length: int,
    ) -> tuple[float, ...] | None:
        if result is None:
            return None

        if isinstance(result, tuple):
            for item in reversed(result):
                values = self._coerce_float_sequence(item, minimum_length)
                if values is not None:
                    return values
        return self._coerce_float_sequence(result, minimum_length)

    def _extract_beam_peak_result(self, result) -> tuple[float, float] | None:
        if result is None:
            return None

        if isinstance(result, tuple):
            if len(result) >= 3 and isinstance(result[0], (bool, int)):
                if not result[0]:
                    raise StaadConnectionError("Failed to read beam deflection from OpenSTAAD.")
                try:
                    return float(result[1]), float(result[2])
                except (TypeError, ValueError):
                    return None

            numeric_values = [item for item in result if isinstance(item, (int, float))]
            if len(numeric_values) >= 2:
                return float(numeric_values[-2]), float(numeric_values[-1])

        return None

    def _coerce_float_sequence(self, value, minimum_length: int) -> tuple[float, ...] | None:
        if not isinstance(value, (list, tuple)):
            return None
        if len(value) < minimum_length:
            return None

        try:
            return tuple(float(item) for item in value)
        except (TypeError, ValueError):
            return None

    def _model_length_to_mm_factor(self, unit: str | None) -> float:
        if unit is None:
            return 1.0
        normalized = unit.strip().upper()
        return self._MODEL_LENGTH_TO_MM.get(normalized, 1.0)

    def _beam_property_length_to_model_factor(self, model_unit: str | None) -> float:
        model_to_mm = self._model_length_to_mm_factor(model_unit)
        if model_to_mm <= 0.0:
            return 1.0
        # OpenSTAAD beam section properties are returned in meters, while the
        # parsed .std geometry stays in the model's working length unit.
        return 1000.0 / model_to_mm
